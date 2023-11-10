using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{

    public abstract class SerializationAttribute : Attribute
    {
        public readonly bool IsFixedSize;
        public readonly int? FixedSize;
        public readonly Type Type;
        

        public SerializationAttribute(int? fixedSize, Type t)
        {
            IsFixedSize = fixedSize != null; FixedSize = fixedSize; Type = t;
        }

    }

    public abstract class SerializationMethodAttribute : SerializationAttribute
    {
        public readonly bool IsSerializer;

        public SerializationMethodAttribute(int? fixedSize, Type t, bool isSerializer) : base (fixedSize, t) 
        {
           IsSerializer = isSerializer;
        }
    }

    public abstract class SerializationAttribute<T> : SerializationMethodAttribute
    {
        public SerializationAttribute(int? fixedSize, bool isSerializer) : base(fixedSize, typeof(T), isSerializer) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CustomSerializationAttribute<T> : SerializationAttribute<T>
    {
        public CustomSerializationAttribute(): base(null, true) { }
        public CustomSerializationAttribute(int fixedSize) : base(fixedSize, true) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class CustomDeserializationAttribute<T> : SerializationAttribute<T>
    {
        public CustomDeserializationAttribute() : base(null, false) { }
        public CustomDeserializationAttribute(int fixedSize) : base(fixedSize, false) { }
    }
}
