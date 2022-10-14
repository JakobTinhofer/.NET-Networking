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

        

        public override bool KeepMessagesInOrder { get; set; }

        private class UdpMultiPacket
        {
            public static ArrayPool<byte> bufferPool = ArrayPool<byte>.Shared;

            public UdpMultiPacket(int frames, byte[] buffer)
            {
                _frames = frames; bufferToReturn = buffer;
            }

            private int _frames;
            public int FramesRemaining { get { return _frames; } set { _frames = value; if (_frames <= 0) bufferPool.Return(bufferToReturn); } }
            private byte[] bufferToReturn;

        }

        private async Task Deconstruct(byte[] packet, int length, bool mode, UDPPacketArgs args)
        {
            await Task.Run(() => {
                int index = 0;
                int payload_len;
                

                UdpMultiPacket up = new UdpMultiPacket(0, packet);

                while (index < length)
                {
                    payload_len = BinaryPrimitives.ReadInt32LittleEndian(new ReadOnlySpan<byte>(packet, index, 4));
                    index += 4;

                    if (payload_len > 0)
                    {
                        ReadOnlyMemory<byte> payload = new ReadOnlyMemory<byte>(packet, index, payload_len);
                        if (KeepMessagesInOrder)
                        {
                            PacketHandler?.Invoke(payload.Span, args);
                        }
                        else
                        {
                            up.FramesRemaining += 1;
                            Task.Run(() => { PacketHandler?.Invoke(payload.Span, args); up.FramesRemaining--; });
                        }

                        index += payload_len;
                    }
                }
            });
        }


        private byte[] syncBuffer = new byte[60000];
        protected async override void StartListening()
        {
            await Task.Run(() => {
                bool mode = KeepMessagesInOrder;
                byte[] buffer = KeepMessagesInOrder ? syncBuffer : UdpMultiPacket.bufferPool.Rent(60000);
                while (true)
                {
                    
                    try
                    {
                        
                        EndPoint ep = _remoteEndpoint ?? new IPEndPoint(IPAddress.Any, 0);
                        int len = Socket.ReceiveFrom(syncBuffer, ref ep);
                        
                        
                        if (_remoteEndpoint == null || _remoteEndpoint == ep)
                        {
                            if (len > 0) Deconstruct(syncBuffer, len, mode, new UDPPacketArgs(this, _remoteEndpoint != null, ep)).Wait();
                        }


                        mode = KeepMessagesInOrder;
                        if (!mode && len > 0) buffer = UdpMultiPacket.bufferPool.Rent(60000);
                    }
                    catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException || ex is ConnectionDeconstructedException)
                    {
                        CallConnectionClosed(ex);
                        return;
                    }
                    
                }
            });
        }

        protected override void WriteToSocket(byte[] data)
        {
            try
            {
                if (_remoteEndpoint == null) throw new InvalidOperationException("Cannot use Write without knowing the recipient! Either set a default recipient, or use WriteTo!");
                Socket.SendTo(data, _remoteEndpoint);
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException || ex is ConnectionDeconstructedException)
            {
                CallConnectionClosed(ex);
            }
        }

        public void WriteTo(byte[] data, EndPoint endPoint)
        {
            try
            {
                Socket.SendTo(data, endPoint);
            }
            catch (Exception ex) when (ex is SocketException || ex is ObjectDisposedException || ex is ConnectionDeconstructedException)
            {
                CallConnectionClosed(ex);
            }
        }


        //TODO: WriteTo; ReadFrom (i.e. multicast server)
    }
}
