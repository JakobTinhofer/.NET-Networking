using System.Reflection;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    public abstract class SerializationMethodAttribute : Attribute
    {
        /// <summary>
        /// Defines whether the decorated method is a serializer or deserializer method.
        /// </summary>
        public readonly bool IsSerializer;
        /// <summary>
        /// Describes whether or not the type to be (de)serialized can be packed into a fixed size.
        /// </summary>
        public readonly bool IsFixedSize;
        /// <summary>
        /// If <see cref="IsFixedSize"/>, contains the serialized size of the type, otherwise null.
        /// </summary>
        public readonly int? FixedSize;

        internal SerializationMethodAttribute(int? fixedSize, bool isSerializer)
        {
            IsSerializer = isSerializer;
            FixedSize = fixedSize;
            IsFixedSize = fixedSize != null;
        }

        /// <summary>
        /// Checks whether the method is a valid (de)serializer.
        /// </summary>
        /// <param name="m">The method marked as (de)serializer.</param>
        /// <param name="isSerializer">Check as serializer or deserialize?</param>
        /// <exception cref="ArgumentException">Invalid method, check failed.</exception>
        public static void CheckValid(MethodInfo m, bool isSerializer)
        {
            if (!m.IsStatic || m.GetParameters().Length != 1 || m.ReturnType == typeof(void)) throw new ArgumentException("All (de)serializers need to be static & take 1 arg & have a non-void return type.");
            if (isSerializer && m.ReturnType != typeof(byte[])) throw new ArgumentException("Invalid since return type of serializers needs to be byte.");
            if (!isSerializer && m.GetParameters()[0].ParameterType != typeof(ReadOnlyMemory<byte>)) throw new ArgumentException("Invalid since first parameter needs to be of type ReadOnlyMemory<byte>.");
        }

        /// <summary>
        /// Get the (de)serialized type of the (de)serializer method.
        /// </summary>
        public Type CheckSerializerType(MethodInfo m)
        {
            CheckValid(m, IsSerializer);
            return IsSerializer ? m.GetParameters()[0].ParameterType : m.ReturnType;
        }
    }

    /// <summary>
    /// Mark this method as a serializer for a type. The method needs to be static and take the value to serialize as the only parameter, returning a byte array.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AtomicSerializerAttribute : SerializationMethodAttribute
    {
        public AtomicSerializerAttribute() : base(null, true) { }
        /// <summary>
        /// Use this if the type can always be represented by the same number of bytes.
        /// </summary>
        /// <param name="fixedSize">The number of bytes needed to serialize this type.</param>
        public AtomicSerializerAttribute(int fixedSize) : base(fixedSize, true) { }
    }

    /// <summary>
    /// Mark this method as a deserializer for a type. The method needs to be static and take a <see cref="ReadOnlyMemory{byte}"/> to deserialize as the only parameter, returning the deserialized object/value.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class AtomicDeserializerAttribute : SerializationMethodAttribute
    {
        public AtomicDeserializerAttribute() : base(null, false) { }
        /// <summary>
        /// Use this if the type can always be represented by the same number of bytes.
        /// </summary>
        /// <param name="fixedSize">The number of bytes needed to serialize this type.</param>
        public AtomicDeserializerAttribute(int fixedSize) : base(fixedSize, false) { }
    }
}
