using LightBlueFox.Connect.Util;
using System.Net;


namespace LightBlueFox.Connect.Net
{
    internal class UDPPacketArgs : MessageArgs
    {
        public readonly bool WasFixedReceiver;
        public EndPoint SenderEndpoint;

        public UDPPacketArgs(UdpConnection sender, bool fr, EndPoint ep) : base(sender)
        {
            WasFixedReceiver = fr; SenderEndpoint = ep;
        }
    }
}
