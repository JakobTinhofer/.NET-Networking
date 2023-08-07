namespace LightBlueFox.Connect.Util
{
    /// <summary>
    /// A class that carries important metadata associated with a certain message. Is passed to all <see cref="MessageHandler"/> - implementations.
    /// </summary>
    public class MessageArgs
    {
        /// <summary>
        /// Create a new message args object
        /// </summary>
        /// <param name="sender">See <see cref="MessageArgs.Sender"/></param>
        public MessageArgs(Connection sender)
        {
            Sender = sender;
        }

        /// <summary>
        /// The connection endpoint that received the message.
        /// </summary>
        public readonly Connection Sender;

    }
}
