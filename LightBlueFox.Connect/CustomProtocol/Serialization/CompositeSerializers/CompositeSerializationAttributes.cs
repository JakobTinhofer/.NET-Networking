using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class ForceSerializationAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public class DontSerializeAttribute : Attribute { }


    public abstract class CompositeSerializeAttribute : SerializationAttribute
    {
        protected CompositeSerializeAttribute(int? fixedSize, Type t) : base(fixedSize, t)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class CompositeSerializeAttribute<T> : CompositeSerializeAttribute
    {
        public CompositeSerializeAttribute(int fixedSize) : base(fixedSize, typeof(T))
        {
        }

        public CompositeSerializeAttribute() : base(null, typeof(T)) { }
    }

    [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
    public class UseConstructorAttribute : Attribute { }

    [AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false)]
    public class AssignAttribute : Attribute {
        public readonly string FieldName;

        public AssignAttribute(string fieldName)
        {
            FieldName = fieldName;
        }
    }
}
