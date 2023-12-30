namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    /// <summary>
    /// Thrown when a <see cref="CompositeLibraryEntry{T}"/> needs an unknown serializer/deserializer for a certain field <see cref="Type"/>.
    /// </summary>
    public class MissingSerializationDependencyException : Exception
    {
        public readonly Type MissingDependency;
        public MissingSerializationDependencyException(Type t) : base("Missing serialization dependency: " + t + ". There are no serializers for this type in the given SerializationLibrary.")
        {
            MissingDependency = t;
        }
    }
}
