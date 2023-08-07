using LightBlueFox.Connect.Util;
using System.Buffers;
using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;

namespace LightBlueFox.Connect.Net
{


    public class TcpConnection : NetworkConnection
    {

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
        public TcpConnection(string ip, int port) : this(new TcpClient(ip, port)) {
        
        }

        /// <summary>
        /// Just writes data to socket stream
        /// </summary>
        protected override void WriteToSocket(ReadOnlyMemory<byte> data)
        {
            byte[] sizePrefix = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(sizePrefix, data.Length);
            Socket.Send(sizePrefix);
            Socket.Send(data.Span);
        }

        #region Reading

        /// <summary>
        /// Describes how a buffer is to be handled after it was completely received.
        /// </summary>
        /// <param name="buffer">The received buffer.</param>
        private delegate ReadState BufferAction(ReadOnlyMemory<byte> buffer, MessageReleasedHandler finishedHandling);

        /// <summary>
        /// Describes a buffer that needs to be filled and an action that should be performed once the buffer has been filled.
        /// </summary>
        private class ReadState
        {
            /// <summary>
            /// The actual data read.
            /// </summary>
            public Memory<byte> Buffer;
            public MessageReleasedHandler DoFree;


            public int Length;

            /// <summary>
            /// The index to which will be written next. If this index is equal to the length of <see cref="ReadState.Buffer"/>, the read is finished and <see cref="OnBufferFilled"/> will be called.
            /// </summary>
            public int WriteIndex = 0;

            private static MemoryPool<byte> messageBufferPool = MemoryPool<byte>.Shared;

            

            /// <summary>
            /// Creates a new readstate representing the next message to be read.
            /// </summary>
            /// <param name="buffer">The actual buffer rented from the ArrayPool.</param>
            /// <param name="Length">A length equal or smaller that the size of the buffer. This is how many bytes will actually be read.</param>
            /// <param name="action"></param>
            public ReadState(int Length, BufferAction action)
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
            public ReadState(byte[] buffer, BufferAction action)
            {
                Buffer = buffer;
                OnBufferFilled = action;
                Length = 4;
                DoFree = (b,c) => { };
            }

            /// <summary>
            /// The action to perform with the finished buffer.
            /// </summary>
            public BufferAction OnBufferFilled;
        }
        
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
                    CallConnectionClosed(ex);
                }
                

            });
        }

        #region Message Actions

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
            MessageReceived(message, finishedHandling);
            return new ReadState(sizeBuffer, SizePrefixAction);
        }

        #endregion

        

        #endregion
    }
}
