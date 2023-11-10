using LightBlueFox.Connect.CustomProtocol.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.CustomProtocol
{

    public struct MyStructType
    {
        int Number;
        string Data;
    }

    public enum MyEnum
    {
        FirstValue,
        SecondValue,
        ThirdValue
    }

    [TestClass]
    public class CustomSerializers
    {
        [CustomSerialization<MyEnum>(1)]
        public static byte[] SerializeEnum(MyEnum val) => new byte[1] { (byte)val };

        [CustomDeserialization<MyEnum>(1)]
        public static MyEnum DeserializeEnum(ReadOnlyMemory<byte> data) => (MyEnum)data.Span[0];


        [TestMethod]
        public void testCustomSer()
        {
            SerializationLibrary l = new SerializationLibrary();
            l.AddSerializers(this.GetType());

            var b = l.Serialize(MyEnum.SecondValue);
            Assert.IsTrue(l.Deserialize<MyEnum>(b) == MyEnum.SecondValue);
        }

    }
}
