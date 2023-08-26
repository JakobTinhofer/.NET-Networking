using System.Net;
using System.Net.Sockets;

namespace LightBlueFox.Connect.Net.ConnectionSources
{
    public class TcpSource : NetworkConnectionSource
    {
        public TcpSource(Socket listener) : base(listener)
        {
        }

        public TcpSource(int port, IPAddress? address = null) : base(port, SocketProtocol.TCP, address)
        {
        }

        protected override NetworkConnection CreateConnection(Socket s)
        {
            return new TcpConnection(s);
        }
    }
}
