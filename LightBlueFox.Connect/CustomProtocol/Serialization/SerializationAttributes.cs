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

    public abstract class AtomicSerializationAttribute<T> : SerializationMethodAttribute
    {
        public AtomicSerializationAttribute(int? fixedSize, bool isSerializer) : base(fixedSize, typeof(T), isSerializer) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AtomicSerializerAttribute<T> : AtomicSerializationAttribute<T>
    {
        public AtomicSerializerAttribute(): base(null, true) { }
        public AtomicSerializerAttribute(int fixedSize) : base(fixedSize, true) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AtomicDeserializerAttribute<T> : AtomicSerializationAttribute<T>
    {
        public AtomicDeserializerAttribute() : base(null, false) { }
        public AtomicDeserializerAttribute(int fixedSize) : base(fixedSize, false) { }
    }
}
