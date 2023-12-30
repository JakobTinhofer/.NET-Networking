using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// This attribute targets a class or struct as a message type. 
    /// Message types need to follow the following rules:
    /// * No Constructor (will maybe be implemented at a later date, see NR-14)
    /// * Properties might cause unexpected behavior
    /// * Field types that all need to have serializers in the serialization library.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class MessageAttribute : CompositeSerializeAttribute
    {
        public MessageAttribute() { }
    }
}
