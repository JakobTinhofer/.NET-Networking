using System.Net;
using System.Net.Sockets;

namespace LightBlueFox.Connect.Net.ConnectionSources
{
    public class UdpSource : NetworkConnectionSource
    {
        public UdpSource(Socket listener) : base(listener)
        {
        }

        public UdpSource(int port, IPAddress? address = null) : base(port, SocketProtocol.UDP, address)
        {
        }

        protected override NetworkConnection CreateConnection(Socket s)
        {
            return new UdpConnection(s, (IPEndPoint)(s.RemoteEndPoint ?? throw new InvalidOperationException("Socket remote endpoint null when trying to create udp connection!")));
        }
    }
}
