namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    /// <summary>
    /// Allows the generation of dynamic serializers for this type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class CompositeSerializeAttribute : Attribute
    {
        public CompositeSerializeAttribute() { }
    }
}
