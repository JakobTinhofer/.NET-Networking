using System.Reflection;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{

    /// <summary>
    /// Describes a method that can handle a message of type T.
    /// </summary>
    /// <typeparam name="T">Any type tagged with the <see cref="MessageAttribute"/>.</typeparam>
    /// <param name="message">The deserilized message.</param>
    /// <param name="args">Context like sender <see cref="ProtocolConnection"/>.</param>
    public delegate void ProtocolMessageHandler<T>(T message, MessageInfo args);

    /// <summary>
    /// Tags a method as a message handler. The method signature needs to fit <see cref="ProtocolMessageHandler{T}"/>, where T is inferred from the first method parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class MessageHandlerAttribute : Attribute
    {
        /// <summary>
        /// Checks if the given method fullfils the <see cref="ProtocolMessageHandler{T}"/> requirements, then inferes and returns T.
        /// Requirements:
        ///     * Needs to be static
        ///     * Needs to take 2 Params:
        ///         - param 1: T -> will be interpreted as the message type for the handler, needs to have a MessageAttribute attached.
        ///         - param 2: MessageInfo
        /// </summary>
        /// <param name="m">>The method info of the method to check.</param>
        /// <returns>The MessageType of the method, inferred from the first parameter.</returns>
        /// <exception cref="ArgumentException">Thrown for invalid methods.</exception>
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
