using System.Net;
using System.Net.Sockets;

namespace LightBlueFox.Connect.Net
{
    /// <summary>
    /// Describes a connection between to machines over the Internet Protocol (IP).
    /// </summary>
    public abstract class NetworkConnection : Connection
    {
        /// <summary>
        /// Create a new Connection from an existing socket.
        /// </summary>
        /// <param name="s">The already established Socket. Make sure <see cref="Socket.ProtocolType"/> is either <see cref="ProtocolType.Udp"/> or <see cref="ProtocolType.Tcp"/>.</param>
        /// <param name="remoteEndpoint">The endpoint of the remote client.</param>
        /// <exception cref="ArgumentException">The given arguments were invalid. This is likely because the given Socket uses an invalid protocol.</exception>
        public NetworkConnection(Socket s, IPEndPoint remoteEndpoint)
        {
            if ((s.ProtocolType != ProtocolType.Tcp && s.ProtocolType != ProtocolType.Udp)) throw new ArgumentException("This is not the right protocol. Make sure that your socket uses either udp or tcp!");
            Protocol = (SocketProtocol)s.ProtocolType;
            _socket = s;
            _remoteEndpoint = remoteEndpoint;
            KeepMessagesInOrder = true;
        }

        #region Fields & Properties

        #region Socket
        /// <summary>
        /// The os socket this connection is based on.
        /// </summary>
        protected Socket Socket {get{return _socket;}}
        private Socket _socket;
        #endregion

        #region RemoteEndpoint
        /// <summary>
        /// The endpoint (as seen from the perspective of this device) of the remote client.
        /// Keep in mind, this might not be the actual endpoint of the device (If the connection is using a relay server,
        /// this will be the EndPoint of the mirror server, not the client on the other side)
        /// </summary>
        public IPEndPoint RemoteEndpoint { get { return (IPEndPoint)_remoteEndpoint; } }
        private EndPoint _remoteEndpoint;
        #endregion

        /// <summary>
        /// The underlying protocol of this connection.
        /// </summary>
        public readonly SocketProtocol Protocol;

        #endregion

        #region Abstracts
        /// <summary>
        /// Start listening on the current <see cref="NetworkConnection.Socket"/>
        /// </summary>
        protected abstract void StartListening();
        #endregion

        #region Writing

        private Mutex writeMutex = new(); // Ensures that no two threads try writing to the same socket at the same time.

        /// <summary>
        /// Ask the connection to write a new packet to the socket.
        /// </summary>
        public override void WriteMessage(ReadOnlyMemory<byte> Packet)
        {
            if (IsClosed) throw new InvalidOperationException("Socket is closed.");
            if (Protocol == SocketProtocol.UDP && Packet.Length + 4 > 59900) throw new ArgumentException("A udp message may not be over 59900 bytes in length!");
            writeMutex.WaitOne();
            WriteToSocket(Packet);
            writeMutex.ReleaseMutex();
        }

        /// <summary>
        /// This does the protocol-specific write to the socket.
        /// </summary>
        protected abstract void WriteToSocket(ReadOnlyMemory<byte> data);

        #endregion

        #region Overrides
        /// <summary>
        /// Closes the connection and disposes the socket. This will likely trigger the <see cref="Connection.ConnectionDisconnected"/> event with the exception of type <see cref="ObjectDisposedException"/>.
        /// </summary>
        public override void CloseConnection()
        {
            try
            {
                Socket.Shutdown(SocketShutdown.Both);
                Socket.Close();
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException) { }
            CallConnectionDisconnected(null);
            IsClosed = true;
        }
        #endregion
    }
}
