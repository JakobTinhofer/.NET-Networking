using LightBlueFox.Connect.Util;

namespace LightBlueFox.Connect
{
    
    /// <summary>
    /// This describes any connection between two points, be it over the internet or whatever other communication media.
    /// </summary>
    public abstract class Connection
    {
        /// <summary>
        /// Initializes the read queue for the new connection.
        /// </summary>
        protected Connection()
        {
            ReadQueue = new MessageQueue(handleReadQueue);
        }

        #region Fields & Properties
        
        #region MessageHandlerProperty
        /// <summary>
        /// A method which handles any incoming messages. If set null, handling is paused and messages are queued up.
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
        private MessageHandler? _handler;
        #endregion

        /// <summary>
        /// Describes whether or not the current connection is terminated.
        /// </summary>
        public bool IsClosed { get; protected set; } = false;

        #endregion

        #region Events
        /// <summary>
        /// Gets called when the connection is terminated. An exception is provided as the reason for the disconnection.
        /// </summary>
        public event ConnectionDisconnectedHandler? ConnectionDisconnected;
        #endregion

        #region Abstract Components
        /// <summary>
        /// Sends a packet to the program at the other side.
        /// </summary>
        /// <param name="Packet">Just the packet data, no size prefix required.</param>
        public abstract void WriteMessage(ReadOnlyMemory<byte> Packet);

        /// <summary>
        /// End the connection at this moment. Might fire <see cref="Connection.ConnectionDisconnected"/>.
        /// </summary>
        public abstract void CloseConnection();
        #endregion

        #region Protected Methods
        /// <summary>
        /// Informs the application was terminated by calling <see cref="Connection.ConnectionDisconnected"/>.
        /// </summary>
        /// <param name="ex"></param>
        protected void CallConnectionDisconnected(Exception? ex)
        {
            Task.Run(() => ConnectionDisconnected?.Invoke(this, ex));
        }

        /// <summary>
        /// Called whenever a new message is received. This will then choose what to do with the message (queuing, handling, etc.).
        /// </summary>
        /// <param name="message">The byte representation of the message.</param>
        /// <param name="finished">Callback for when the message finished handling and the memory can be released.</param>
        protected void MessageReceived(ReadOnlyMemory<byte> message, MessageReleasedHandler? finished)
        {
            if (MessageHandler == null || KeepMessagesInOrder) ReadQueue.Add(new(message, finished));
            else Task.Run(() => { MessageHandler.Invoke(message.Span, new(this)); finished?.Invoke(message, this); });
        }
        #endregion

        #region Message Queuing
        /// <summary>
        /// This flag controls whether to allow concurrent handling of messages. If true, every message handler needs to return before the next message handler is called.
        /// </summary>
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
        } // Read Queue action.
        private MessageQueue ReadQueue;
        #endregion

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