using LightBlueFox.Connect.Net;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.Net
{
    public static class Helpers
    {
        #region Test Data

        static Random random = new Random();
        public static List<byte>[] GenerateRandomData(int nr, int maxSize, out int bytes, int seed = 0)
        {
            return GeneratePatternedData(nr, maxSize, out bytes, (r) => { return (byte)r.Next(0, 255); }, seed);
        }
        public delegate byte Patterner(Random r);
        public static List<byte>[] GeneratePatternedData(int nr, int maxSize, out int bytes, Patterner p, int seed = 0)
        {
            bytes = 0;
            if (seed != 0) random = new Random(seed);
            List<byte>[] data = new List<byte>[nr];
            for (int i = 0; i < nr; i++)
            {
                data[i] = new List<byte>();
                int l = random.Next(1, maxSize);
                for (int j = 0; j < l; j++)
                {
                    data[i].Add(p(random));
                }
                bytes += l;
            }
            return data;
        }

        public static List<byte>[] GenerateConsistentData(int nr, int maxSize, out int bytes, int seed)
        {
            return GeneratePatternedData(nr, maxSize, out bytes, (r) => { return 111; }, seed);
        }

        public static List<byte>[] GenerateRandomData(int nr, int maxSize, int seed = -1)
        {
            int b = 0;
            return GenerateRandomData(nr, maxSize, out b, seed);
        }

        #endregion

        #region Connection Sources

        public static (NetworkConnection, NetworkConnection) GetTcpConnections(int port)
        {
            TcpListener l = new TcpListener(IPAddress.Any, port);
            l.Start();
            var t = l.AcceptSocketAsync();
            NetworkConnection c = new TcpConnection("127.0.0.1", port);
            return (c, new TcpConnection(t.Result));
        }

        public static (UdpConnection, UdpConnection) GetUdpConnections(int port)
        {
            UdpClient c = new UdpClient(port, AddressFamily.InterNetwork);
            UdpClient c2 = new UdpClient("127.0.0.1", port);
            IPEndPoint c2_rem_ep = (IPEndPoint)(c2.Client.RemoteEndPoint ?? throw new InvalidOperationException("Udp socket local ep is null!"));
            IPEndPoint c_rem_ep = new IPEndPoint(IPAddress.Any, port);
            Task<bool> r = Task.Run(() =>
            {
                return c.Receive(ref c_rem_ep)[0] == 222;

            });

            c2.Send(new byte[1] { 222 });
            if (!r.GetAwaiter().GetResult()) throw new InvalidOperationException("Invalid packet received. Why?");

            return (new UdpConnection(c.Client, c_rem_ep),
                    new UdpConnection(c2.Client, c2_rem_ep));
        }

        #endregion



        #region Helpers and Comparers
        public static bool Compare(byte[] e1, byte[] e2)
        {


            if (e1.Length != e2.Length) return false;

            for (int i = 0; i < e1.Length; i++)
            {
                if (e1[i] != e2[i]) return false;
            }
            return true;
        }

        public static bool CompareL(List<byte>[] e1, List<byte>[] e2)
        {
            if (e1.Length != e2.Length) return false;

            for (int i = 0; i < e1.Length; i++)
            {
                if (!Compare(e1[i].ToArray(), e2[i].ToArray())) return false;
            }
            return true;
        }

        public static bool TestConsistency(byte[] b)
        {

            foreach (var bt in b)
            {
                if (bt != 111) return false;
            }

            return true;
        }
        #endregion
    }
}
