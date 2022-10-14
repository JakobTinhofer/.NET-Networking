using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
{
    internal class UDPPacketArgs : PacketArgs
    {
        public readonly bool WasFixedReceiver;
        public EndPoint SenderEndpoint;

        public UDPPacketArgs(UdpConnection sender, bool fr, EndPoint ep) : base(sender)
        {
            WasFixedReceiver = fr; SenderEndpoint = ep;
        }
    }
}
