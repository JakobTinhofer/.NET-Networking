using Microsoft.VisualStudio.TestTools.UnitTesting;
using LightBlueFox.Networking;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Linq;
using System.Net;
using System.Threading;

namespace NetworkTests
{
    [TestClass]
    public class ConnectionTest
    {
        

        #region Tests

        #region TCP
        [TestMethod]
        public void TestTcpConnection_SinglePacket()
        {
            (NetworkConnection sender, NetworkConnection receiver) = Helpers.GetTcpConnections(12312);
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();
            

            var d = Helpers.GenerateRandomData(1, 40000);

            receiver.MessageHandler = (e, args) => { 
                tcs.SetResult(e.ToArray());
            };
            sender.WritePacket(d[0].ToArray());
            Assert.IsTrue(Helpers.Compare(tcs.Task.GetAwaiter().GetResult(), d[0].ToArray()));
            sender.CloseConnection();
            
        }
        [TestMethod]
        public void TestTcpConnection_MultiPacket()
        {
            (NetworkConnection sender, NetworkConnection receiver) = Helpers.GetTcpConnections(12312);
            (receiver as TcpConnection ?? throw new Exception()).KeepMessagesInOrder = true;

            var d = Helpers.GenerateRandomData(500, 40000);

            TaskCompletionSource<List<byte>[]> tcs = new TaskCompletionSource<List<byte>[]>();

            List<List<byte>> receivedPackets = new List<List<byte>>();
            receiver.MessageHandler = (e, args) =>
            {
                receivedPackets.Add(e.ToArray().ToList());
                if(receivedPackets.Count == d.Length) tcs.SetResult(receivedPackets.ToArray());
            };
            foreach (var item in d)
            {
                sender.WritePacket(item.ToArray());
            }
            Assert.IsTrue(Helpers.CompareL(tcs.Task.GetAwaiter().GetResult(), d));
            sender.CloseConnection();
        }
        #endregion

        #region UDP
        [TestMethod]
        public void TestUdpConnection_SinglePacket()
        {
            (UdpConnection sender, UdpConnection receiver) = Helpers.GetUdpConnections(12321);
            TaskCompletionSource<byte[]> tcs = new TaskCompletionSource<byte[]>();

            var d = Helpers.GenerateRandomData(1, 30000);

            receiver.MessageHandler = (e, args) =>
            {
                tcs.SetResult(e.ToArray());
            };
            sender.WritePacket(d[0].ToArray());
            Assert.IsTrue(Helpers.Compare(tcs.Task.GetAwaiter().GetResult(), d[0].ToArray()));
            sender.CloseConnection();
        }

        [TestMethod]
        public void TestUdpConnection_MultiPacket()
        {
            (UdpConnection sender, UdpConnection receiver) = Helpers.GetUdpConnections(12322);
            TaskCompletionSource<int> tcs = new TaskCompletionSource<int>();

            int nrOfBytes = 0;
            var d = Helpers.GenerateConsistentData(666, 30000, out nrOfBytes, 0);

            int nrOfPackets = 0;
            receiver.MessageHandler = (e, args) =>
            {
                Assert.IsTrue(Helpers.TestConsistency(e.ToArray()));
                nrOfPackets++;
                nrOfBytes -= e.Length;
            };
            foreach (var item in d)
            {
                sender.WritePacket(item.ToArray());
            }
            sender.CloseConnection();
        }
        #endregion
        #endregion

        
        
    }
}