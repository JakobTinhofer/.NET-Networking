using System.Runtime.CompilerServices;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    /// <summary>
    /// Allows for the serialization of enums as their underlying numeric type.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EnumSerializationEntry<T> : SerializationLibraryEntry<T> where T : Enum
    {
        private static DeserializerDelegate<T> BuildDeserialize(SerializationLibrary l)
        {
            var enumType = typeof(T).GetEnumUnderlyingType();
            return (mem) =>
            {
                return (T)l.Deserialize(mem, enumType);
            };
        }

        private static SerializerDelegate<T> BuildSerialize(SerializationLibrary l)
        {
            var enumType = typeof(T).GetEnumUnderlyingType();
            return (e) =>
            {
                return l.Serialize(Convert.ChangeType(e, enumType));
            };
        }

        /// <summary>
        /// Creates a new entry for <typeparamref name="T"/>.
        /// </summary>
        /// <param name="sl">A serialization library to look up the serializers for the underyling numeric type.</param>
        public EnumSerializationEntry(SerializationLibrary sl) : base(Unsafe.SizeOf<T>(), (BuildSerialize(sl), BuildDeserialize(sl))) { }
    }
}
