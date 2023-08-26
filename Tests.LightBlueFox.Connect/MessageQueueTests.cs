using Microsoft.VisualStudio.TestTools.UnitTesting;
using LightBlueFox.Connect.Util;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect
{
    /// <summary>
    /// Tests for the <code>LightBlueFox.Connect.Util.MessageQueue</code> class.
    /// </summary>
    [TestClass]
    public class MessageQueueTests
    {
        [TestMethod]
        public void PerformMessageQueuePause()
        {
            int ctr = 2;
            bool expectingPassthrough = true;
            MessageQueueActionHandler h = (MessageStoreHandle s) => {
                Debug.WriteLine("Message dequeued.");
                ctr--;
                Assert.IsTrue(expectingPassthrough);
            };

            MessageQueue queue = new(h);
            queue.WorkOnQueue = true;

            queue.Add(new MessageStoreHandle());

            queue.WorkOnQueue = false;
            expectingPassthrough = false;

            Debug.WriteLine("illegal message");
            queue.Add(new MessageStoreHandle());

            expectingPassthrough = true;
            queue.WorkOnQueue = true;
            
            
            Thread.Sleep(300);
            Assert.IsTrue(ctr == 0);
        }

        [TestMethod]
        public void TwoParallelQueueTests()
        {
            int q1Expc = 1, q2Expc = 1;
            MessageQueue q1 = new MessageQueue((MessageStoreHandle s) => { q1Expc--; });
            MessageQueue q2 = new MessageQueue((MessageStoreHandle s) => { q2Expc--; });

            q1.WorkOnQueue = true;
            q2.WorkOnQueue = true;

            Thread.Sleep(40);

            Task[] tasks2 = new Task[2] { Task.Run(() => { q1.WorkOnQueue = false; }), Task.Run(() => { q2.WorkOnQueue = false; }) };



            Task[] tasks = new Task[2] { Task.Run(() => { q1.WorkOnQueue = true; }), Task.Run(() => { q2.WorkOnQueue = true; }) };
            

            q1.Add(new MessageStoreHandle());
            q2.Add(new MessageStoreHandle());

            Thread.Sleep(50);
            Assert.IsTrue(q1Expc == q2Expc && q1Expc == 0, "Expected number of dequeued messages does not align with actual number of messages: Q1: {0}, Q2: {1}", q1Expc, q2Expc);
        }

        [TestMethod]
        [Timeout(TestTimeout.Infinite)]
        public void CancelingItsselfQueueTest()
        {
            AutoResetEvent? ev = null;
            AutoResetEvent waitDumb = new(false);
            MessageQueue? q = null;
            q = new((MessageStoreHandle s) => {
                if (q == null) return;
                if(ev == null)
                {
                    q.WorkOnQueue = false;
                    q.WorkOnQueue = true;
                    waitDumb.Set();
                }
                else
                {
                    ev.Set();
                }
            });

            q.WorkOnQueue = true;

            q.Add(new());
            waitDumb.WaitOne();
            ev = new AutoResetEvent(false);
            q.Add(new());
            ev.WaitOne();
        }


    }
}
