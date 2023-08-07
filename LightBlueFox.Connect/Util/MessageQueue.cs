using System.Collections.Concurrent;

namespace LightBlueFox.Connect.Util
{
    /// <summary>
    /// A class that represents a thread-safe FIFO datastructure, which allows datastreams from multiple threads/sources to be consomed in order by a singe consumer. Also, if there is no consumer set yet, data will be buffered until there is.
    /// </summary>
    internal class MessageQueue
    {
        /// <summary>
        /// Creates and sets up a new message queue with a <see cref="MessageQueueActionHandler"/> for processing the dequeued messages.
        /// </summary>
        /// <param name="queueAction"></param>
        public MessageQueue(MessageQueueActionHandler queueAction)
        {
            QueueAction = queueAction;
        }

        #region Fields
        /// <summary>
        /// What to do with the dequeued messages. Note that if ever set to null, messages will be lost. Use <see cref="MessageQueue.WorkOnQueue"/> to pause the queue.
        /// </summary>
        public MessageQueueActionHandler QueueAction;


        private BlockingCollection<MessageStoreHandle> storedMessages = new();
        private Task? QueueWorkerTask = null;
        private CancellationTokenSource stopTakingMessages = new();

        /// <summary>
        /// While this is true, messages will continuosly be dequeued. If set to false, dequeuing will pause until set to true again.
        /// </summary>
        public bool WorkOnQueue
        {
            get
            {
                return QueueWorkerTask != null;
            }
            set
            {
                if (value && QueueWorkerTask == null) QueueWorkerTask = Task.Run(QueueWorker);
                else if (!value && QueueWorkerTask != null) stopTakingMessages.Cancel();
            }
        }

        #endregion

        #region I/O
        private void QueueWorker()
        {
            var token = stopTakingMessages.Token;
            while (!token.IsCancellationRequested)
            {
                MessageStoreHandle msg;
                try
                {
                    msg = storedMessages.Take(token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }

                QueueAction?.Invoke(msg);

            }
            QueueWorkerTask = null;

        }

        public void Add(MessageStoreHandle message)
        {
            storedMessages.Add(message);
        }

        #endregion
    }

    /// <summary>
    /// Describes an action that should be taken once a message is dequeued from the message queue. This usually involves doing something with the message and then calling <see cref="MessageStoreHandle.FinishedHandling"/>.
    /// </summary>
    /// <param name="storedMessage">The message that was dequeued.</param>
    public delegate void MessageQueueActionHandler(MessageStoreHandle storedMessage);
}
