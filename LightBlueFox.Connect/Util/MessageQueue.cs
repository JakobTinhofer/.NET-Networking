using System.Collections.Concurrent;
using System.Diagnostics;

namespace LightBlueFox.Connect.Util
{
    /// <summary>
    /// A class that represents a thread-safe FIFO datastructure, which allows datastreams from multiple threads/sources to be consomed in order by a singe consumer. Also, if there is no consumer set yet, data will be buffered until there is.
    /// </summary>
    public class MessageQueue
    {
        /// <summary>
        /// Creates and sets up a new message queue with a <see cref="MessageQueueActionHandler"/> for processing the dequeued messages.
        /// </summary>
        /// <param name="queueAction"></param>
        public MessageQueue(MessageQueueActionHandler queueAction)
        {
            QueueAction = queueAction;
            token = stopTakingMessages.Token;
        }

        #region Fields
        /// <summary>
        /// What to do with the dequeued messages. Note that if ever set to null, messages will be lost. Use <see cref="MessageQueue.WorkOnQueue"/> to pause the queue.
        /// </summary>
        public MessageQueueActionHandler QueueAction;


        private BlockingCollection<MessageStoreHandle> storedMessages = new();
        private Task? QueueWorkerTask = null;
        private CancellationTokenSource stopTakingMessages = new();
        private CancellationToken token;
        /// <summary>
        /// While this is true, messages will continuously be dequeued. If set to false, dequeuing will pause until set to true again.
        /// </summary>
        public bool WorkOnQueue
        {
            get
            {
                return QueueWorkerTask != null;
            }
            set
            {   
                if(token.IsCancellationRequested)
                {
                    workerCanceled(token);
                }
                
                alteringQueueWorker.WaitOne();
                if (value && QueueWorkerTask == null)
                {
                    QueueWorkerTask = Task.Run(() => { QueueWorker(token); });
                }
                else if (!value && QueueWorkerTask != null)
                {
                    stopTakingMessages.Cancel(true);
                }
                else
                {
                    alteringQueueWorker.Set();
                }
            }
        }


       

        private AutoResetEvent alteringQueueWorker = new(true);
        #endregion

        #region I/O
        private void QueueWorker(CancellationToken myToken)
        {
            alteringQueueWorker.Set();
            while (!myToken.IsCancellationRequested)
            {
                try
                {
                    MessageStoreHandle msg;
                    msg = storedMessages.Take(token);
                    QueueAction?.Invoke(msg);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
            }
            workerCanceled(myToken);
        }

        private void workerCanceled(CancellationToken t)
        {
            if (t != token) return;
            var id = QueueWorkerTask?.Id;
            QueueWorkerTask = null;
            stopTakingMessages = new CancellationTokenSource();
            token = stopTakingMessages.Token;
            alteringQueueWorker.Set();
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
