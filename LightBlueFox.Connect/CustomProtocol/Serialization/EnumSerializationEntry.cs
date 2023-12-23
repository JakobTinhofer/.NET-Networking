using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    public class EnumSerializationEntry<T> : SerializationLibraryEntry<T> where T : Enum
    {
        private static DeserializerDelegate<T> BuildDeserialize(BaseSerializationLibrary l)
        {
            var enumType = typeof(T).GetEnumUnderlyingType();
            return (mem) =>
            {
                return (T)l.Deserialize(mem, enumType);
            };
        }

        private static SerializerDelegate<T> BuildSerialize(BaseSerializationLibrary l)
        {
            var enumType = typeof(T).GetEnumUnderlyingType();
            return (e) =>
            {
                return l.Serialize(Convert.ChangeType(e, enumType));
            };
        }

        public EnumSerializationEntry(BaseSerializationLibrary sl) : base(Unsafe.SizeOf<T>(), (BuildSerialize(sl), BuildDeserialize(sl))) { }
    }
}
