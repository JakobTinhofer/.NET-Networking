using System.Reflection;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{

    public abstract class SerializationAttribute : Attribute
    {
        public readonly bool IsFixedSize;
        public readonly int? FixedSize;

        public SerializationAttribute(int? fixedSize)
        {
            IsFixedSize = fixedSize != null; FixedSize = fixedSize;
        }

    }

    public abstract class SerializationMethodAttribute : SerializationAttribute
    {
        public readonly bool IsSerializer;

        public SerializationMethodAttribute(int? fixedSize, bool isSerializer) : base(fixedSize)
        {
            IsSerializer = isSerializer;
        }

        public static void CheckValid(MethodInfo m, bool isSerializer)
        {
            if (!m.IsStatic || m.GetParameters().Length != 1 || m.ReturnType == typeof(void)) throw new ArgumentException("All (de)serializers need to be static & take 1 arg & have a non-void return type.");
            if (isSerializer && m.ReturnType != typeof(byte[])) throw new ArgumentException("Invalid since return type of serializers needs to be byte.");
            if (!isSerializer && m.GetParameters()[0].ParameterType != typeof(ReadOnlyMemory<byte>)) throw new ArgumentException("Invalid since first parameter needs to be of type ReadOnlyMemory<byte>.");
        }

        public Type CheckSerializerType(MethodInfo m)
        {
            CheckValid(m, IsSerializer);
            return IsSerializer ? m.GetParameters()[0].ParameterType : m.ReturnType;
        }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AtomicSerializerAttribute : SerializationMethodAttribute
    {
        public AtomicSerializerAttribute() : base(null, true) { }
        public AtomicSerializerAttribute(int fixedSize) : base(fixedSize, true) { }
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class AtomicDeserializerAttribute : SerializationMethodAttribute
    {
        public AtomicDeserializerAttribute() : base(null, false) { }
        public AtomicDeserializerAttribute(int fixedSize) : base(fixedSize, false) { }
    }
}
