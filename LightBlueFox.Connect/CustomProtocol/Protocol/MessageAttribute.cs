using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    [AttributeUsage(AttributeTargets.Struct)]
    public class MessageAttribute : CompositeSerializeAttribute
    {
        public readonly bool HandlesExceptions;

        public MessageAttribute(bool handlesExceptions = false)
        {
            HandlesExceptions = handlesExceptions;
        }
    }

    public abstract class AnswerableMessageAttribute : MessageAttribute
    {
        public readonly Type AnswerType;

        protected AnswerableMessageAttribute(Type ansType) : base(true)
        {
            AnswerType = ansType;
        }
    }

    public class MessageAttribute<T> : AnswerableMessageAttribute
    {
        public MessageAttribute() : base(typeof(T)) { }
    }
}
