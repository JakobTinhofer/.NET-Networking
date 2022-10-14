using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking.BasicProtocols.StructSerializer
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    internal class KnownSizeAttribute : Attribute
    {
        public readonly int Size;
        public KnownSizeAttribute(int size)
        {
            if (size > 0) Size = size;
            else throw new ArgumentException("Size needs to be larger than 0!");
        }
    }
}
