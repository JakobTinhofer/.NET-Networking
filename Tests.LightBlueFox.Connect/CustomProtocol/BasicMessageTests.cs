using LightBlueFox.Connect;
using LightBlueFox.Connect.CustomProtocol.Protocol;
using LightBlueFox.Connect.CustomProtocol.Serialization;
using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Connect.Structure;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.CustomProtocol
{
    [TestClass]
    public class BasicMessageTests
    {

        [TestMethod]
        public void simpleMsgTest()
        {
            SerializationLibrary l = new();
            ProtocolDefinition def = new ProtocolDefinition(l, typeof(TestMessages));
            ProtocolConnection? p1 = null; 
            ProtocolConnection? p2 = null;

            var pair = TestConnection.CreatePair();
            Task[] t = new Task[2]{
                Task.Run(() => { p1 = ProtocolConnection.CreateWithValidation(def, pair.Item1, ConnectionNegotiationPosition.Authorizer); }),
                Task.Run(() => { p2 = ProtocolConnection.CreateWithValidation(def, pair.Item2, ConnectionNegotiationPosition.Challenger); })
            };
            Task.WaitAll(t);

            TestMessages.TestMessage1.tcs = new();

            p1.WriteMessage(new TestMessages.TestMessage1() { TestData = new[] { "a", "bc", "def" }, testValue = 5 });
            var res = TestMessages.TestMessage1.tcs.Task.GetAwaiter().GetResult();
            Assert.IsTrue(res.TestData[2] == "def");

            TestMessages.TestMessage2.tcs = new();

            p2.WriteMessage(new TestMessages.TestMessage2() { TestData = new[] {1, 4, 7, 9} , testValue = 5.123f });
            var res2 = TestMessages.TestMessage2.tcs.Task.GetAwaiter().GetResult();
            Assert.IsTrue(res2.testValue == 5.123f);
        }



    }
}
