using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices.Marshalling;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
{
    public class UdpConnection : NetworkConnection
    {
        public UdpConnection(Socket s, IPEndPoint remoteEndpoint) : base(s, remoteEndpoint)
        {
            StartListening();
        }

        /// <summary>
        ///
        /// </summary>
        private byte[] syncBuffer = new byte[60000];
        private static ArrayPool<byte> messageBufferPool = ArrayPool<byte>.Shared;
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

                        MessageReceived(new ReadOnlyMemory<byte>(newBuff, 0, len), (b, c) => { messageBufferPool.Return(newBuff); });
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
        /// Writes a single datagram
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
