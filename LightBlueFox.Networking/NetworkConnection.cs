using System;
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
    /// <summary>
    /// Describes a connection between to machines over the Internet Protocol (IP).
    /// </summary>
    public abstract class NetworkConnection : Connection
    {
        #region Private Fields
        /// <summary>
        /// The socket that is used to communicate to the remote client with.
        /// </summary>
        protected Socket Socket;
        protected EndPoint _remoteEndpoint;
        protected bool _keepMessagesInOrder;



        #endregion

        #region Public Properties
        /// <summary>
        /// The endpoint (as seen from the perspective of this device) of the remote client.
        /// Keep in mind, this might not be the actual endpoint of the device (If the connection is using a relay server,
        /// this will be the EndPoint of the mirror server, not the client on the other side)
        /// </summary>
        public IPEndPoint RemoteEndpoint { get { return (IPEndPoint)_remoteEndpoint; } }
        public Protocol Protocol { get; protected set; }

        public abstract bool KeepMessagesInOrder { get; set; }
        #endregion

        #region Constructors

        /// <summary>
        /// Create a new Connection from an existing socket.
        /// </summary>
        /// <param name="s">The already established Socket. Make sure <see cref="Socket.ProtocolType"/> is either <see cref="ProtocolType.Udp"/> or <see cref="ProtocolType.Tcp"/>.</param>
        /// <param name="re">The endpoint of the remote client.</param>
        /// <exception cref="ArgumentException">The given arguments were invalid. This is likely because the given Socket uses an invalid protocol.</exception>
        public NetworkConnection(Socket s, IPEndPoint re)
        {
            if ((s.ProtocolType != ProtocolType.Tcp && s.ProtocolType != ProtocolType.Udp)) throw new ArgumentException("This is not the right protocol. Make sure that your socket uses either udp or tcp!");
            Protocol = (Protocol)s.ProtocolType;
            Socket = s;
            _remoteEndpoint = re;
            OnQueueFlushRequested += FlushWriteQueue;
            KeepMessagesInOrder = true;
        }

        #endregion

        protected abstract void StartListening();

        private Mutex BlockSocketWrite = new Mutex(false);

        private ConcurrentQueue<ReadOnlyMemory<byte>> writeMessages = new ConcurrentQueue<ReadOnlyMemory<byte>>();



        #region Write Queue Flushing

        /// <summary>
        /// If this many elements are reached in the write queue, a compound packet is sent.
        /// </summary>
        public static int WriteQueueMaxElements = 5;

        /// <summary>
        /// 
        /// </summary>
        public static int WriteQueueFlushInterval = 5;

        static NetworkConnection()
        {
            QueueFlusher();
        }

        private static async void QueueFlusher()
        {
            while (true)
            {
                await Task.Delay(WriteQueueFlushInterval);
                if(OnQueueFlushRequested != null)
                    await Task.Run(OnQueueFlushRequested.Invoke);
            }
        }
        private delegate void QueueFlushRequestedHandler();
        private static event QueueFlushRequestedHandler? OnQueueFlushRequested;

        #endregion

        private Mutex _sendMutex = new Mutex();
        private async void FlushWriteQueue()
        {
            await Task.Run(() => {
                if(writeMessages.Count > 0)
                {
                    using (MemoryStream ms = new MemoryStream())
                    {
                        ReadOnlyMemory<byte> message;
                        _sendMutex.WaitOne();
                        while (writeMessages.TryDequeue(out message))
                        {
                            // Stay within some safe packet size limits. If we dequeue too much, push it back in (this should preserve the order)
                            if(Protocol == Protocol.UDP &&  ms.Length + message.Length + 4 > 59900)
                            {
                                writeMessages.Prepend(message);
                                break;
                            }
                            Span<byte> sizeSpan = new byte[4];
                            BinaryPrimitives.WriteInt32LittleEndian(sizeSpan, message.Length);
                            ms.Write(sizeSpan);
                            ms.Write(message.Span);
                        }
                        try
                        {
                            WriteToSocket(ms.ToArray());
                        }
                        catch (ConnectionDeconstructedException)
                        {
                            if(OnQueueFlushRequested?.GetInvocationList().Contains(FlushWriteQueue) ?? false)
                                OnQueueFlushRequested -= FlushWriteQueue;
                        }
                        _sendMutex.ReleaseMutex();
                    }

                }
            });
        }
        public override void WritePacket(ReadOnlyMemory<byte> Packet)
        {
            if (Protocol == Protocol.UDP && Packet.Length + 4 > 59900) throw new ArgumentException("A udp message may not be over 59900 bytes in length!");
            writeMessages.Enqueue(Packet);
            if (writeMessages.Count > WriteQueueMaxElements) FlushWriteQueue();
        }

        protected abstract void WriteToSocket(byte[] data);

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
            IsClosed = true;
        }

        public bool IsClosed { get; private set; } = false;

        public Socket DuplicateConnectionSocket()
        {
            Socket s = new Socket(Socket.AddressFamily, Socket.SocketType, Socket.ProtocolType);
            s.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            s.Bind(Socket.LocalEndPoint ?? throw new InvalidOperationException("Cannot duplicate unbound socket!"));
            return s;
        }
    }



    public enum Protocol
    {
        TCP = 6,
        UDP = 17,
    }
}
