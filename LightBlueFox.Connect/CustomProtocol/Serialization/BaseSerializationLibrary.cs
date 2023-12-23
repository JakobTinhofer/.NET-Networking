using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    public abstract class BaseSerializationLibrary
    {
        public abstract SerializationLibraryEntry<T> GetEntry<T>();
        public abstract SerializationLibraryEntry GetEntry(Type t);

        protected abstract List<Type> GetTypeList();

        protected byte[] GetTypeListHash()
        {
            throw new NotImplementedException();
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            return GetEntry<T>().Deserialize(data);
        }

        public byte[] Serialize<T>(T obj)
        {
            return GetEntry<T>().Serialize(obj);
        }

        public object Deserialize(ReadOnlyMemory<byte> data, Type t)
        {
            return GetEntry(t).DeserializerPointer.DynamicInvoke(data) ?? throw new InvalidOperationException("Deserializer invoke did not return object!");
        }

        public byte[] Serialize(object o)
        {
            return GetEntry(o.GetType()).SerializerPointer.DynamicInvoke(o) as byte[] ?? throw new InvalidOperationException("Serializer invoke did not return bytes!");
        }


    }
}
