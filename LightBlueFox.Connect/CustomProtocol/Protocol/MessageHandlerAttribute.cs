using LightBlueFox.Connect.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    
    public delegate void ProtocolMessageHandler<T>(T message, MessageInfo args);
    public delegate A ProtocolMessageHandler<T, A>(T message, MessageInfo args);

    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        public static Type ValidateAndGetHandlerType(MethodInfo m)
        {
            if (!m.IsStatic) throw new ArgumentException("MessageHandlers need to be static!");
            var param = m.GetParameters();
            if (param.Length != 2) throw new ArgumentException("Not a valid handler, does not take 2 params.");
            if (param[1].ParameterType != typeof(MessageInfo)) throw new ArgumentException("Second parameter needs to be of type MessageInfo.");
            var paramType = param[0].ParameterType;
            var attrs = paramType.GetCustomAttributes<MessageAttribute>();
            if (attrs.Count() != 1) throw new ArgumentException("First parameter needs to have exactly one MessageAttribute!");
            return paramType;
        }

    }
}
