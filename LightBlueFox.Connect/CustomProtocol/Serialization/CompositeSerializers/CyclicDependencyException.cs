using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    public class CyclicDependencyException: Exception
    {
        public CyclicDependencyException(Type t1, Type t2) : base("Type " + t1 + " and " + t2 + " are dependent on each other. Cyclic dependencies are not supported at this moment") {
            
        }
    }
}
