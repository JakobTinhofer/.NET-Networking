using LightBlueFox.Connect.CustomProtocol.Serialization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tests.LightBlueFox.Connect.CustomProtocol
{
    [TestClass]
    public class ArraySerializers
    {

        [TestMethod]
        public void FixedSizeArrayTests()
        {
            var arr = new int[] { 1321231, 2323, 445445, -23233, 0, 231 };
            SerializationLibrary l = new SerializationLibrary();
            var bytes = l.Serialize(arr);
            var newArr = l.Deserialize<int[]>(bytes);
            Assert.IsTrue(Enumerable.SequenceEqual(arr, newArr));
        }

        [TestMethod]
        public void DynamicSizeArrayTests()
        {
            var arr = new string[] { "uiohoih", "§", "ääüöü", "" };
            SerializationLibrary l = new SerializationLibrary();
            var bytes = l.Serialize(arr);
            var newArr = l.Deserialize<string[]>(bytes);
            Assert.IsTrue(Enumerable.SequenceEqual(arr, newArr));
        }

        [TestMethod]
        public void MultiDimensionArrayTest()
        {
            var arr = new int[][] { new int[] { 1, 2, 3 }, new int[] { 1, 2, 3 }, new int[] { -500, 124124, int.MaxValue, 0, 12, 5 }, new int[] { } };
            SerializationLibrary l = new SerializationLibrary();
            var bytes = l.Serialize(arr);
            var newArr = l.Deserialize<int[][]>(bytes);
            for(int i = 0; i <  arr.Length; i++)
            {
                Assert.IsTrue(Enumerable.SequenceEqual(arr[i], newArr[i]));
            }
        }
    }
}
