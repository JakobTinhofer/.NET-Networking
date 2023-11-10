using LightBlueFox.Connect.CustomProtocol.Serialization;
using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.CustomProtocol
{
    [TestClass]
    public class CompositeSerializers
    {
        [TestMethod]
        public void TestBasicValueType()
        {
            SerializationLibrary l = new SerializationLibrary();

            l.AddSerializers(typeof(TestStruct));
            var v = new TestStruct();
            var buff = l.Serialize(v);
            var deser = l.Deserialize<TestStruct>(buff);
            Assert.IsTrue(v.Equals(deser));
        }

        [TestMethod]
        public void TestBasicReferenceType()
        {
            SerializationLibrary l = new SerializationLibrary();

            l.AddSerializers(typeof(TestClass));
            var v = new TestClass();
            var buff = l.Serialize(v);
            var deser = l.Deserialize<TestClass>(buff);
            Assert.IsTrue(v.Equals(deser));
        }

        [TestMethod]
        public void TestDependancy()
        {
            SerializationLibrary l = new();

            Assert.ThrowsException<MissingSerializationDependencyException>(() => { l.AddSerializers(typeof(DependantClass)); });

            l.AddSerializers(typeof(DependantClass), typeof(TestStruct));
            var c = new DependantClass();
            var b = l.Serialize(c);
            Assert.IsTrue(c.Equals(l.Deserialize<DependantClass>(b)));
        }

        [TestMethod]
        public void TestCyclicDependency()
        {
            SerializationLibrary l = new();

            Assert.ThrowsException<CyclicDependencyException>(() => { l.AddSerializers(typeof(Cyclic1), typeof(Cyclic2)); });
        }

        [TestMethod]
        public void TestArrayOfCustom()
        {
            TestClass[] arr = new[] { new TestClass(), new TestClass() };
            SerializationLibrary l = new();

            l.AddSerializers(typeof(TestClass));
            var buff = l.Serialize(arr);
            var deser = l.Deserialize<TestClass[]>(buff);
            Assert.IsTrue(Enumerable.SequenceEqual(arr, deser));
        }


        [CompositeSerialize<TestStruct>]
        private struct TestStruct : IEquatable<TestStruct>
        {
            int fixedSize = 3;
            string dynSize = "hello";

            int[] intArr = { 1234, 321, -231};
            string[] stringArr = { "asdf", "3sadf", "sss"};

            public TestStruct()
            {
            }

            public bool Equals(TestStruct other)
            {
                return other.fixedSize == fixedSize && String.Equals(dynSize, other.dynSize) && Enumerable.SequenceEqual(intArr, other.intArr) && Enumerable.SequenceEqual(stringArr, other.stringArr);
            }
        }

        [CompositeSerialize<TestClass>]
        private class TestClass : IEquatable<TestClass>
        {
            int fixedSize = 3;
            string dynSize = "hello";

            int[] intArr = { 1234, 321, -231 };
            string[] stringArr = { "asdf", "3sadf", "sss" };

            public TestClass()
            {
                fixedSize = 123;
                dynSize = "NOOO";
                intArr = new[] { 333, 44, 55 };
                stringArr = new[] { "fas", "asd", "aa" }; 
            }

            public bool Equals(TestClass? other)
            {
                if (other == null) return false;
                return other.fixedSize == fixedSize && String.Equals(dynSize, other.dynSize) && Enumerable.SequenceEqual(intArr, other.intArr) && Enumerable.SequenceEqual(stringArr, other.stringArr);
            }
        }


        [CompositeSerialize<DependantClass>]
        private class DependantClass : IEquatable<DependantClass>
        {
            TestStruct dependency;
            int otherVal = 4;

            public DependantClass() {
                dependency = new TestStruct();
                otherVal = 9;
            }

            public bool Equals(DependantClass? other)
            {
                if(other == null) return false;
                return other.dependency.Equals(dependency) && other.otherVal == otherVal;
            }
        }

        [CompositeSerialize<Cyclic1>]
        private class Cyclic1
        {
            Cyclic2 dependency;
        }

        [CompositeSerialize<Cyclic2>]
        private class Cyclic2
        {
            Cyclic1 dependency;
        }
    }
}
