using LightBlueFox.Connect.CustomProtocol.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.CustomProtocol
{
    internal class TestMessages
    {
        [Message]
        public struct TestMessage1
        {
            public double testValue;
            public string[] TestData;

            public static TaskCompletionSource<TestMessage1>? tcs;

            [MessageHandler]
            public static void HandleTestMessage1(TestMessage1 testMessage1, MessageInfo i)
            {
                if (tcs != null) tcs.SetResult(testMessage1);
            }
        }

        [Message]
        public struct TestMessage2
        {
            public float testValue;
            public int[] TestData;

            public static TaskCompletionSource<TestMessage2>? tcs;

            [MessageHandler]
            public static void HandleTestMessage2(TestMessage2 testMessage2, MessageInfo i)
            {
                if(tcs != null) tcs.SetResult(testMessage2);
            }
        }
    }
}
