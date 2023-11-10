using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    public class MissingSerializationDependencyException : Exception
    {
        public readonly Type MissingDependency;
        public MissingSerializationDependencyException(Type t) : base("Missing serialization dependency: " + t + ". There are no serializers for this type in the given SerializationLibrary.")
        {
            MissingDependency = t;
        }
    }
}
