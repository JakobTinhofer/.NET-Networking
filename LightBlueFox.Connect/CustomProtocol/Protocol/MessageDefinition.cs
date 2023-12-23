using LightBlueFox.Connect.CustomProtocol.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public class MessageDefinition
    {
        public readonly Type MessageType;
        public readonly TypeID MessageTypeID;
        public readonly string Name;
        public readonly Delegate Handler;
        public readonly Type? AnswerType;
        public bool RequiresAnswer
        {
            get => AnswerType != null;
        }
        public readonly bool AllowExceptions;
        public readonly Byte ID;
        
        public MessageDefinition(Type MessageType, MessageAttribute attribute, Delegate handler, Byte id)
        {
            AnswerType = (attribute as AnswerableMessageAttribute)?.AnswerType;
            AllowExceptions = attribute.HandlesExceptions;
            Name = MessageType.Name;
            Handler = handler;
            this.MessageType = MessageType;
            ID = id;
            MessageTypeID = new(MessageType);
        }
    }

}
