using LightBlueFox.Connect.CustomProtocol.Serialization;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// Stores information about a certain message type.
    /// </summary>
    public class MessageDefinition
    {
        public readonly Type MessageType;
        public readonly TypeID MessageTypeID;
        
        public readonly string Name;
        public readonly Delegate Handler;
        
        public readonly Byte ID;
        
        public MessageDefinition(Type MessageType, MessageAttribute attribute, Delegate handler, Byte id)
        {
            Name = MessageType.Name;
            Handler = handler;
            this.MessageType = MessageType;
            ID = id;
            MessageTypeID = new(MessageType);
        }
    }

}
