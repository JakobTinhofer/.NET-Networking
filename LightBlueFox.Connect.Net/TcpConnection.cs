using LightBlueFox.Connect.Util;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace LightBlueFox.Connect.Net
{
    /// <summary>
    /// A <see cref="NetworkConnection"/> utilizing the <see cref="SocketProtocol.TCP"/> protocol.
    /// </summary>
    public class TcpConnection : NetworkConnection
    {
        #region Constructors
        /// <summary>
        /// Creates a new <see cref="TcpConnection"/> from an existing <see cref="Socket"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <exception cref="ArgumentException">Thrown if <see cref="Socket.SocketType"/> is not <see cref="SocketType.Stream"/> or <see cref="Socket.ProtocolType"/> is not <see cref="ProtocolType.Tcp"/>.</exception>
        /// <exception cref="ArgumentNullException">Thrown if <see cref="Socket.RemoteEndPoint"/> is null.</exception>
        public TcpConnection(Socket s) : base(s, (IPEndPoint)(s.RemoteEndPoint ?? throw new ArgumentNullException("s.RemoteEndpoint")))
        {
            if (s.ProtocolType != ProtocolType.Tcp || s.SocketType != SocketType.Stream) throw new ArgumentException("The given socket does not seem to be a tcp socket!");
            if (s.Connected == false) throw new ArgumentException("The given socket is not connected!");
            StartListening();
        }

        /// <summary>
        /// Create a new tcp connection from an already established tcp client
        /// </summary>
        /// <param name="c"></param>
        public TcpConnection(TcpClient c) : this(c.Client) { }

        /// <summary>
        /// Create a connection to a given ip and port.
        /// </summary>
        public TcpConnection(string ip, int port) : this(new TcpClient(ip, port)) { }
        #endregion

        #region Reading

        #region Read Types
        /// <summary>
        /// Describes what to do once the <see cref="ReadState"/>'s buffer has been filled.
        /// </summary>
        /// <param name="buffer">The received buffer.</param>
        private delegate ReadState ReadStateAction(ReadOnlyMemory<byte> buffer, MessageReleasedHandler finishedHandling);

        /// <summary>
        /// The current state of the reading thread. Describes a buffer that needs to be filled and an action that should be performed once the buffer has been filled.
        /// </summary>
        private class ReadState
        {
            private static MemoryPool<byte> messageBufferPool = MemoryPool<byte>.Shared;

            #region Constructors
            /// <summary>
            /// Creates a new readstate.
            /// </summary>
            /// <param name="Length">How many bytes to read to complete this status.</param>
            /// <param name="action">What to do once all bytes have been read.</param>
            public ReadState(int Length, ReadStateAction action)
            {
                var owner = messageBufferPool.Rent(Length);
                Buffer = owner.Memory;
                DoFree = (m, c) => { owner.Dispose(); };
                this.Length = Length;
                OnBufferFilled = action;
            }

            /// <summary>
            /// Creates a new readstate that represents a length prefix to be read.
            /// </summary>
            /// <param name="buffer"></param>
            /// <param name="action"></param>
            public ReadState(byte[] buffer, ReadStateAction action)
            {
                Buffer = buffer;
                OnBufferFilled = action;
                Length = 4;
                DoFree = null;
            }
            #endregion

            #region Fields
            /// <summary>
            /// The memory where the read data is written to.
            /// </summary>
            public readonly Memory<byte> Buffer;

            /// <summary>
            /// Describes the process for freeing the message memory, if needed.
            /// </summary>
            public readonly MessageReleasedHandler? DoFree;

            /// <summary>
            /// Sets the number of bytes that need to be read to complete the current state.
            /// </summary>
            public readonly int Length;

            /// <summary>
            /// The index to which will be written next. If this index is equal to <see cref="ReadState.Length"/>, the read is finished and <see cref="OnBufferFilled"/> will be called.
            /// </summary>
            public int WriteIndex = 0;

            /// <summary>
            /// The action to perform with the finished buffer.
            /// </summary>
            public ReadStateAction OnBufferFilled;
            #endregion

        }
        #endregion

        private byte[] sizeBuffer = new byte[4];
        protected async override void StartListening()
        {
            await Task.Run(() => {
                ReadState state = new ReadState(sizeBuffer, SizePrefixAction);
                try
                {
                    while (true)
                    {

                        int bytesRead = Socket.Receive(state.Buffer.Slice(0, state.Length).Span);
                        if (bytesRead > 0)
                        {
                            state.WriteIndex += bytesRead;
                            if (state.WriteIndex == state.Length)
                            {
                                state = state.OnBufferFilled(state.Buffer.Slice(0, state.Length), state.DoFree);
                            }
                        }
                    }
                }
                catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
                {
                    CallConnectionDisconnected(ex);
                }


            });
        } // Starts listening on the socket

        #region ReadState Actions

        /// <summary>
        /// Converts the size prefix into an int and rents a buffer from the ArrayPool to which the message will be read.
        /// </summary>
        /// <param name="prefix">The bytes encoding the length prefix.</param>
        private ReadState SizePrefixAction(ReadOnlyMemory<byte> prefix, MessageReleasedHandler finishedHandling)
        {
            int len = BinaryPrimitives.ReadInt32LittleEndian(prefix.Span);
            // TODO: Set max packet length!
            return new ReadState(len, MessageAction);
        }

        /// <summary>
        /// Calls the <see cref="MessageHandler"/>.
        /// </summary>
        private ReadState MessageAction(ReadOnlyMemory<byte> message, MessageReleasedHandler finishedHandling)
        {
            MessageReceived(message, new(this), finishedHandling);
            return new ReadState(sizeBuffer, SizePrefixAction);
        }

        #endregion

        #endregion

        #region Writing
        /// <summary>
        /// Writes data to socket stream
        /// </summary>
        protected override void WriteToSocket(ReadOnlyMemory<byte> data)
        {
            byte[] sizePrefix = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(sizePrefix, data.Length);
            Socket.Send(sizePrefix);
            Socket.Send(data.Span);
        }
        #endregion

    }
}
