using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class CompositeSerializeAttribute : SerializationAttribute
    {
        public CompositeSerializeAttribute(int fixedSize) : base(fixedSize)
        {
        }

        public CompositeSerializeAttribute() :base(null) { }
    }
}
