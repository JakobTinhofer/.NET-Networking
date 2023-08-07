using System.Buffers;
using System.Net;
using System.Net.Sockets;

namespace LightBlueFox.Connect.Net
{
    /// <summary>
    /// A <see cref="NetworkConnection"/> utilizing the <see cref="SocketProtocol.UDP"/> protocol.
    /// </summary>
    public class UdpConnection : NetworkConnection
    {
        /// <summary>
        /// Creates a new <see cref="UdpConnection"/> from an existing socket that only listens for messages from a fixed <see cref="IPEndPoint"/>.
        /// </summary>
        /// <param name="s">The existing UDP socket.</param>
        /// <param name="remoteEndpoint">The IPEndPoint that this connection should listen for packets from.</param>
        public UdpConnection(Socket s, IPEndPoint remoteEndpoint) : base(s, remoteEndpoint)
        {
            StartListening();
        }

        #region Reading

        #region Buffers
        private byte[] syncBuffer = new byte[60000];
        private static ArrayPool<byte> messageBufferPool = ArrayPool<byte>.Shared;
        #endregion

        protected async override void StartListening()
        {
            await Task.Run(() => {
                bool mode = KeepMessagesInOrder;
                while (true)
                {

                    try
                    {

                        EndPoint ep = RemoteEndpoint ?? new IPEndPoint(IPAddress.Any, 0);

                        int len = Socket.ReceiveFrom(syncBuffer, ref ep);
                        if (len == 0) continue;
                        if (RemoteEndpoint != null && RemoteEndpoint != ep) continue;


                        byte[] newBuff = messageBufferPool.Rent(len);
                        Array.Copy(syncBuffer, 0, newBuff, 0, len);

                        MessageReceived(new ReadOnlyMemory<byte>(newBuff, 0, len), new UdpMessageArgs(this, RemoteEndpoint != null, ep), (b, c) => { messageBufferPool.Return(newBuff); });
                    }
                    catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
                    {
                        CallConnectionDisconnected(ex);
                        return;
                    }

                }
            });
        }
        #endregion

        #region Writing
        /// <summary>
        /// Writes a single datagram to the underlying socket.
        /// </summary>
        protected override void WriteToSocket(ReadOnlyMemory<byte> data)
        {
            try
            {
                if (RemoteEndpoint == null) throw new InvalidOperationException("Cannot use Write without knowing the recipient! Either set a default recipient, or use WriteTo!");
                Socket.SendTo(data.Span, RemoteEndpoint);
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                CallConnectionDisconnected(ex);
            }
        }

        /// <summary>
        /// Writes a datagram to a special recipient ip.
        /// </summary> 
        public void WriteTo(byte[] data, EndPoint endPoint)
        {
            try
            {
                Socket.SendTo(data, endPoint);
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                CallConnectionDisconnected(ex);
            }
        }

        //TODO: WriteTo; ReadFrom (i.e. multicast server)
        #endregion

    }
}
