using LightBlueFox.Connect.CustomProtocol.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.Serialization
{

    public enum MyEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue
    }

    [TestClass]
    public class CustomSerializers
    {

        [TestMethod]
        public void TestUnknownEnum()
        {
            SerializationLibrary l = new SerializationLibrary();

            var b = l.Serialize(MyEnum.SecondValue);
            Assert.IsTrue(l.Deserialize<MyEnum>(b) == MyEnum.SecondValue);
        }

        [TestMethod]
        public void TestNullableType()
        {
            SerializationLibrary l = new SerializationLibrary();
            Random rnd = new Random();

            string? nullable = rnd.Next() > 0.5 ? null : "not null";

            var b = l.Serialize(nullable);
            Assert.AreEqual(nullable, l.Deserialize<string?>(b));
        }
    }
}
