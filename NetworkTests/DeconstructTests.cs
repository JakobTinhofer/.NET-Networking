using LightBlueFox.Networking;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace NetworkTests
{
    [TestClass]
    public class DeconstructTests
    {
        [TestMethod]
        public void TestNetConDeconstruct()
        {
            var datas = Helpers.GenerateRandomData(4, 10);


            for (int r = 0; r < datas.Length; r++)
            {
                Debug.WriteLine("Data" + r + " (" + datas[r].Count + "): " + BitConverter.ToString(datas[r].ToArray()));
            }


            (TcpConnection c1, TcpConnection c2) = ((TcpConnection, TcpConnection))Helpers.GetTcpConnections(1414);
            c1.Name = "c1";
            c2.Name = "c2";
            TaskCompletionSource tcs = new TaskCompletionSource();
            int i = 0;
            
            
            c1.ConnectionDisconnected += (s, e) => { Debug.WriteLine("Deconstructed Socket closed with exception: '" + e + "' and message '" + e.Message + "'."); };
            c1.PacketHandler = (p, e) =>
            {
                Assert.IsTrue(Helpers.Compare(p.ToArray(), datas[0].ToArray()));
            };
            c2.WritePacket(datas[0].ToArray()); ;


            Socket s = c1.Deconstruct();
            c1.PacketHandler = (s, e) => Assert.Fail("Deconstructed client received message!");
            
            TcpConnection revived = new TcpConnection(s);
            revived.Name = "revived";
            PacketHandler ph = (p, e) => { i++; Debug.WriteLine("CMP (" + i + "): " + BitConverter.ToString(p.ToArray()) + " | " + Helpers.Compare(p.ToArray(), datas[i].ToArray()));  if(i == 2) tcs.SetResult(); };
            revived.PacketHandler += ph;
            c2.PacketHandler += ph;

            try
            {
                c1.WritePacket(datas[3].ToArray());
                Assert.Fail("Managed to write data to deconstructed connection!");
            }
            catch (ConnectionDeconstructedException)
            {
            }
            c2.WritePacket(datas[1].ToArray());
            revived.WritePacket(datas[2].ToArray());
            tcs.Task.Wait();

        }
    }
}
