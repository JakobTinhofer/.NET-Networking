using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
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
        protected override void WriteToSocket(byte[] data)
        {
            Socket.Send(data);
        }

        #region Reading

        /// <summary>
        /// Describes how a buffer is to be handled after it was completely received.
        /// </summary>
        /// <param name="buffer">The received buffer.</param>
        private delegate ReadState BufferAction(ReadOnlyMemory<byte> buffer);

        /// <summary>
        /// Describes a buffer that needs to be filled and an action that should be performed once the buffer has been filled.
        /// </summary>
        private class ReadState
        {
            /// <summary>
            /// The actual data read.
            /// </summary>
            public byte[] Buffer;

            public int Length;

            public Memory<byte> Data;

            /// <summary>
            /// The index to which will be written next. If this index is equal to the length of <see cref="ReadState.Buffer"/>, the read is finished and <see cref="OnBufferFilled"/> will be called.
            /// </summary>
            public int WriteIndex = 0;

            private static ArrayPool<byte> messageBufferPool = ArrayPool<byte>.Shared;
            private static ConcurrentDictionary<ReadOnlyMemory<byte>, byte[]> borrowedArrays = new ConcurrentDictionary<ReadOnlyMemory<byte>, byte[]>();

            /// <summary>
            /// Returns the array corresponding to the given buffer.
            /// </summary>
            /// <param name="bytes"></param>
            public static void ReturnMemory(ReadOnlyMemory<byte> bytes)
            {
                byte[] byteArray;
                #pragma warning disable CS8600
                if (borrowedArrays.Remove(bytes, out byteArray))
                    messageBufferPool.Return(byteArray, true);
                #pragma warning restore CS8600
            }

            /// <summary>
            /// Creates a new readstate representing the next message to be read.
            /// </summary>
            /// <param name="buffer">The actual buffer rented from the ArrayPool.</param>
            /// <param name="Length">A length equal or smaller that the size of the buffer. This is how many bytes will actually be read.</param>
            /// <param name="action"></param>
            public ReadState(int Length, BufferAction action)
            {
                Buffer = messageBufferPool.Rent(Length);
                this.Length = Length;
                Data = new Memory<byte>(Buffer, 0, Length);
                borrowedArrays.TryAdd(Data, Buffer);
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
                Data = new Memory<byte>(Buffer, 0, Length);
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
                        int bytesRead = Socket.Receive(state.Buffer, state.WriteIndex, state.Length - state.WriteIndex, 0);
                        if (bytesRead > 0)
                        {
                            state.WriteIndex += bytesRead;
                            if (state.WriteIndex == state.Length)
                            {
                                state = state.OnBufferFilled(state.Data);
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
        private ReadState SizePrefixAction(ReadOnlyMemory<byte> prefix)
        {
            int len = BinaryPrimitives.ReadInt32LittleEndian(prefix.Span);
            // TODO: Set max packet length!
            return new ReadState(len, MessageAction);
        }

        /// <summary>
        /// Calls the <see cref="MessageHandler"/>.
        /// </summary>
        private ReadState MessageAction(ReadOnlyMemory<byte> message)
        {
            if (KeepMessagesInOrder)
            {
                messages.Add(message);
            }
            else
            {
                Task.Run(() => { MessageHandler?.Invoke(message.Span, new MessageArgs(this)); ReadState.ReturnMemory(message); });
            }
            return new ReadState(sizeBuffer, SizePrefixAction);
        }

        #endregion

        #region Message Processing

        private bool _keepMessagesInOrder;
        /// <summary>
        /// If true, the <see cref="MessageHandler"/> needs to return before the next message is processed and the handler is called again. Packets received in the meantime will be queued up.
        /// </summary>
        public override bool KeepMessagesInOrder { 
            get {
                return _keepMessagesInOrder;
            } 
            set {
                if (value && (queueWorker == null || queueWorker.IsCompleted))
                {
                    queueWorker = WorkOnQueue();
                }
                
                _keepMessagesInOrder = value;
            } 
        }


        private BlockingCollection<ReadOnlyMemory<byte>> messages = new BlockingCollection<ReadOnlyMemory<byte>>();
        private Task? queueWorker;
        private async Task WorkOnQueue()
        {
            await Task.Run(() => {
                ReadOnlyMemory<byte> message;
                while (KeepMessagesInOrder || messages.Count > 0){
                    if (messages.TryTake(out message))
                    {
                        MessageHandler?.Invoke(message.Span, new MessageArgs(this));
                        ReadState.ReturnMemory(message);
                    }
                }

            });
        }

        #endregion

        #endregion
    }
}
