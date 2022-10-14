using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

namespace LightBlueFox.Networking.BasicProtocols.StructSerializer
{
    [AttributeUsage(AttributeTargets.Struct, Inherited = false)]
    public class AutoSerializeAttribute : Attribute
    {
        public static 

    }

    public class CompositeSerializableType<T> : SerializableType<T> where T : struct
    {
        public ConstructorInfo? Constructor;
        private List<FieldInfo> Fields = new List<FieldInfo>();

        public override T Deserializer(ReadOnlySpan<byte> s)
        {
            throw new NotImplementedException();
        }

        public override byte[] Serializer(T obj)
        {
            foreach (var f in Fields)
            {
                if(f.FieldType)
            }
        }

        public CompositeSerializableType()
        {
            foreach (var field in typeof(T).GetFields())
            {
                if (field.IsInitOnly || !field.IsPublic) throw new InvalidOperationException("This struct may only contain public and writable or static fields!");
                Fields.Add(field);
            }
        }
    }
}
