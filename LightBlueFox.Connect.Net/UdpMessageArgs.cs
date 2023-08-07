using LightBlueFox.Connect.Util;
using System.Net;


namespace LightBlueFox.Connect.Net
{
    /// <summary>
    /// Describes the metadata associated with a message received over the <see cref="SocketProtocol.UDP"/> protocol.
    /// </summary>
    internal class UdpMessageArgs : MessageArgs
    {
        /// <summary>
        /// Create a new <see cref="UDPPacketArgs"/> object.
        /// </summary>
        /// <param name="sender">The connection that received the datagram.</param>
        /// <param name="fr">Whether the packet was received from a single previously fixed remote endpoint, or just any sender.</param>
        /// <param name="ep">The remote endpoint that sent the datagram.</param>
        public UdpMessageArgs(UdpConnection sender, bool fr, EndPoint ep) : base(sender)
        {
            WasFixedSender = fr; SenderEndpoint = ep;
        }

        /// <summary>
        /// Whether the packet was received from a single previously fixed remote endpoint, or just any sender.
        /// </summary>
        public readonly bool WasFixedSender;
        /// <summary>
        /// The remote endpoint that sent the datagram.
        /// </summary>
        public readonly EndPoint SenderEndpoint;

    }
}
