using LightBlueFox.Connect.Util;

namespace LightBlueFox.Connect
{
    // This describes any connection between two programs, be it over the internet or whatever other communication media.
    public abstract class Connection
    {

        private MessageHandler? _handler;
        /// <summary>
        /// A method which handles any incoming packets
        /// </summary>
        public MessageHandler? MessageHandler 
        {
            get
            {
                return _handler;
            }
            set
            {
                ReadQueue.WorkOnQueue = value != null;
                _handler = value;
            }
        }

        /// <summary>
        /// Gets called when the connection is terminated. An exception is provided as the reason for the disconnection.
        /// </summary>
        public event ConnectionDisconnectedHandler? ConnectionDisconnected;

        /// <summary>
        /// Sends a packet to the program at the other side.
        /// </summary>
        /// <param name="Packet">Just the packet data, no size prefix required.</param>
        public abstract void WriteMessage(ReadOnlyMemory<byte> Packet);

        public abstract void CloseConnection();

        protected void CallConnectionClosed(Exception? ex)
        {
            Task.Run(() => ConnectionDisconnected?.Invoke(this, ex));
        }

        #region Message Queuing

        public bool KeepMessagesInOrder = true;


        private void handleReadQueue(MessageStoreHandle msg)
        {
            var callHandler = () =>
            {
                MessageHandler?.Invoke(msg.Buffer.Span, new(this));
                msg.FinishedHandling?.Invoke(msg.Buffer, this);
            };

            if (KeepMessagesInOrder) callHandler();
            else Task.Run(callHandler);
        }

        private MessageQueue ReadQueue;


        #region Queue Worker

        #endregion

        protected void MessageReceived(ReadOnlyMemory<byte> message, MessageReleasedHandler? finished)
        {
            if (MessageHandler == null || KeepMessagesInOrder) ReadQueue.Add(new(message, finished));
            else Task.Run(() => { MessageHandler.Invoke(message.Span, new(this)); finished?.Invoke(message, this); });
        }

        #endregion

        public Connection()
        {
            ReadQueue = new MessageQueue(handleReadQueue);
        }
    }
    /// <summary>
    /// Describes a method equipped to handle packets coming from an active <see cref="Connection"/>.
    /// </summary>
    /// <param name="sender">The connection which received the packet.</param>
    /// <param name="msg">The raw data that was received.</param>
    public delegate void MessageHandler(ReadOnlySpan<byte> msg, MessageArgs args);

    /// <summary>
    /// Describes methods which can be used to listen to the <see cref="Connection.ConnectionDisconnected"/> event.
    /// </summary>
    /// <param name="sender">The connection that was terminated.</param>
    /// <param name="disconnectionException">The reason for the termination. Null if the connection terminated as planned.</param>
    public delegate void ConnectionDisconnectedHandler(Connection sender, Exception? disconnectionException);
}