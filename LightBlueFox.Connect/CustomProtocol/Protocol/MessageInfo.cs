namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// Contains additional context for a received message, such as sender Information.
    /// </summary>
    public class MessageInfo
    {
        /// <summary>
        /// The <see cref="ProtocolConnection"/> this message was received from.
        /// </summary>
        public readonly ProtocolConnection From;
        internal MessageInfo(ProtocolConnection from)
        {
            From = from;
        }
    }

    
}
