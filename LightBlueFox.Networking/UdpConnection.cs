using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
{
    public class UdpConnection : NetworkConnection
    {
        public UdpConnection(Socket s, IPEndPoint re) : base(s, re)
        {
            StartListening();
        }

        /// <summary>
        /// If true; waits for one message to finish processing before the next one is passed to the handler
        /// </summary>
        public override bool KeepMessagesInOrder { get; set; }

        /// <summary>
        /// Class-Internal representation of one or more messages sent in one datagram
        /// </summary>
        private class UdpMultiMessage
        {
            public static ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

            public UdpMultiMessage(int messages, byte[] buffer)
            {
                _messages = messages; bufferToReturn = buffer;
            }
            /// <summary>
            /// The number of messages still not handled from this datagram.
            /// </summary>
            private int _messages;

            /// <summary>
            /// Makes sure buffer is returned as soon as all messages are handled.
            /// </summary>
            public int MessagesRemaining { get { return _messages; } set { _messages = value; if (_messages <= 0) bufferPool.Return(bufferToReturn); } }
            private byte[] bufferToReturn;
        }

        private async Task Deconstruct(byte[] datagram, int length, bool mode, UDPPacketArgs args)
        {
            await Task.Run(() => {
                int index = 0;
                int payload_len;
                

                UdpMultiMessage up = new UdpMultiMessage(0, datagram);

                while (index < length)
                {
                    payload_len = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(datagram, index, 4));
                    index += 4;

                    if (payload_len > 0)
                    {
                        ReadOnlyMemory<byte> payload = new ReadOnlyMemory<byte>(datagram, index, payload_len);
                        //Need to check mode instead of KeepMessagesInOrder since that could have changed between some of the packets
                        if (mode)
                        {
                            MessageHandler?.Invoke(payload.Span, args);
                        }
                        else
                        {
                            up.MessagesRemaining += 1;
                            Task.Run(() => { MessageHandler?.Invoke(payload.Span, args); up.MessagesRemaining--; });
                        }

                        index += payload_len;
                    }
                }
            });
        }

        /// <summary>
        /// This buffer is used when messages are kept in order (since the buffer can be overwritten once a message has been handled)
        /// If <see cref="KeepMessagesInOrder"/> is false, buffers are rented from an arraypool.
        /// </summary>
        private byte[] syncBuffer = new byte[60000];
        protected async override void StartListening()
        {
            await Task.Run(() => {
                bool mode = KeepMessagesInOrder;
                byte[] buffer = KeepMessagesInOrder ? syncBuffer : UdpMultiMessage.bufferPool.Rent(60000);
                while (true)
                {
                    
                    try
                    {
                        
                        EndPoint ep = RemoteEndpoint ?? new IPEndPoint(IPAddress.Any, 0);
                        int len = Socket.ReceiveFrom(syncBuffer, ref ep);
                        
                        
                        if (RemoteEndpoint == null || RemoteEndpoint == ep)
                        {
                            if (len > 0) Deconstruct(syncBuffer, len, mode, new UDPPacketArgs(this, RemoteEndpoint != null, ep)).Wait();
                        }


                        mode = KeepMessagesInOrder;
                        if (!mode && len > 0) buffer = UdpMultiMessage.bufferPool.Rent(60000);
                    }
                    catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
                    {
                        CallConnectionClosed(ex);
                        return;
                    }
                    
                }
            });
        }

        /// <summary>
        /// Writes one or more messages as a single datagram
        /// </summary>
        protected override void WriteToSocket(byte[] data)
        {
            try
            {
                if (RemoteEndpoint == null) throw new InvalidOperationException("Cannot use Write without knowing the recipient! Either set a default recipient, or use WriteTo!");
                Socket.SendTo(data, RemoteEndpoint);
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                CallConnectionClosed(ex);
            }
        }

        /// <summary>
        /// Writes to a special recipient ip.
        /// </summary>
        public void WriteTo(byte[] data, EndPoint endPoint)
        {
            try
            {
                Socket.SendTo(data, endPoint);
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException)
            {
                CallConnectionClosed(ex);
            }
        }


        //TODO: WriteTo; ReadFrom (i.e. multicast server)
    }
}
