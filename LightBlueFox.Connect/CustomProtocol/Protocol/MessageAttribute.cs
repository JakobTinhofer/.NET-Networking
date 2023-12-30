using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{

    
    [AttributeUsage(AttributeTargets.Struct | AttributeTargets.Class)]
    public class MessageAttribute : CompositeSerializeAttribute
    {
        
        public MessageAttribute()
        {
        }
    }
}
