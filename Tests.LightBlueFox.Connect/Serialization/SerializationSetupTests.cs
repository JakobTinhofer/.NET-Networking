using LightBlueFox.Connect.CustomProtocol.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Linq;

namespace Tests.LightBlueFox.Connect.Serialization
{
    [TestClass]
    public class SerializationSetupTests
    {

        [TestMethod]
        public void SetupDefaultSerialization()
        {
            SerializationLibrary l = new SerializationLibrary();

            byte[] data = l.Serialize(123);
            Assert.IsTrue(l.Deserialize<int>(data) == 123);

            data = l.Serialize("1234üäö");
            Assert.IsTrue(l.Deserialize<string>(data) == "1234üäö");

            data = l.Serialize<byte>(123);
            Assert.IsTrue(l.Deserialize<byte>(data) == 123);

            data = l.Serialize(true);
            Assert.IsTrue(l.Deserialize<bool>(data) == true);

            data = l.Serialize(123456789990);
            Assert.IsTrue(l.Deserialize<long>(data) == 123456789990);

            data = l.Serialize(System.ConsoleColor.Red);
            Assert.IsTrue(l.Deserialize<ConsoleColor>(data) == ConsoleColor.Red);

            Assert.ThrowsException<SerializationEntryNotFoundException>(() =>
            {
                data = l.Serialize(new UnknownType());
            });
        }


        public class UnknownType { }
    }
}
