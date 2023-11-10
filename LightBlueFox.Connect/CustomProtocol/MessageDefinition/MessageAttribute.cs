using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.MessageDefinition
{
    public class MessageAttribute
    {
    }

    public abstract class AnswerableMessageAttribute : MessageAttribute
    {
        public readonly Type AnswerType;

        protected AnswerableMessageAttribute(Type ansType) {
            AnswerType = ansType;
        }
    }

    public class MessageAttribute<T> : AnswerableMessageAttribute
    {
        public MessageAttribute() : base(typeof(T)) { }
    }
}
