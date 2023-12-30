using LightBlueFox.Connect.CustomProtocol.Serialization;
using LightBlueFox.Connect.Util;
using System.Reflection;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// Defines the protocol standard, providing a list of messages and their identifiers.
    /// </summary>
    public class ProtocolDefinition
    {
        private readonly SerializationLibrary Serializations;
        private readonly IReadOnlyDictionary<Type, MessageDefinition> MessageDefinitionsByType = new Dictionary<Type, MessageDefinition>();
        private readonly Dictionary<byte, MessageDefinition> MessageDefinitions = new();

        /// <summary>
        /// Used to make sure both ends of a <see cref="ProtocolConnection"/> adhere to the same definitions.
        /// </summary>
        public readonly ProtocolValidator Validator;

        /// <summary>
        /// Add new serializers, message types and handlers from a list of types. This method will crawl all subtypes and methods.
        /// </summary>
        /// <param name="types">A list of types to parse.</param>
        /// <exception cref="NotImplementedException">Thrown when more than 255 messages are entered.</exception>
        /// <exception cref="ArgumentException">Invalid types detected.</exception>
        /// <exception cref="InvalidOperationException">Thrown if message definitions have no corresponding handlers or the other way around.</exception>
        private void ReadDefinitions(params Type[] types)

        {
            Dictionary<Type, MessageAttribute> tDict = new();
            List<(MethodInfo method, MessageHandlerAttribute attr)> handlers = new();

            Action<MethodInfo, Type> mFilter = (m, t) =>
            {
                var attr = m.GetCustomAttribute<MessageHandlerAttribute>();
                if (m.IsStatic && attr != null)
                {
                    handlers.Add((m, attr));
                }
            };

            Action<Type> tFilter = (t) =>
            {
                var attr = t.GetCustomAttribute<MessageAttribute>();
                if (!t.IsAbstract && attr != null && !MessageDefinitionsByType.ContainsKey(t))
                {
                    tDict.Add(t, attr);
                }

            };

            Serializations.FilterForSerialization(true, tFilter, mFilter, null, types);

            var dictOrdered = tDict.Keys.OrderBy((r) => r.Name + r.Namespace).ToList();

            if (dictOrdered.Count > byte.MaxValue) throw new NotImplementedException("More than " + byte.MaxValue + " msgs are currently not supported!");

            if (handlers.Count < tDict.Count) throw new ArgumentException("Not all message types have handlers!");

            foreach (var h in handlers)
            {
                var hType = MessageHandlerAttribute.ValidateAndGetHandlerType(h.method);

                if (MessageDefinitionsByType.ContainsKey(hType))
                {
                    if (MessageDefinitionsByType[hType].Handler.Method == h.method) continue;
                    throw new InvalidOperationException("Duplicate handlers for message type " + hType);
                }

                if (!tDict.ContainsKey(hType)) throw new InvalidOperationException("Could not find Message of type " + hType + ". Please include enclosing type(s).");

                var entr = tDict[hType];
                var delType = typeof(ProtocolMessageHandler<>).MakeGenericType(hType);
                var indx = (byte)dictOrdered.IndexOf(hType);
                MessageDefinitions.Add(indx, new MessageDefinition(hType, entr, h.method.CreateDelegate(delType), indx));
            }



        }
        /// <summary>
        /// Create a new ProtocolDefinition from an existing SerialzationLibrary with a list of types to crawl for message definitions, handlers and additional serializers. 
        /// </summary>
        /// <param name="sl">The Serialization library with the information to serialize all the field types of the message types.</param>
        /// <param name="t">A collection of types that will be crawled for message definitions/ handlers as well as additional serializers.</param>
        public ProtocolDefinition(SerializationLibrary sl, params Type[] t)
        {
            Serializations = sl;
            ReadDefinitions(t.Append(typeof(DefaultMessages)).ToArray());
            MessageDefinitionsByType = MessageDefinitions.Values.ToDictionary<MessageDefinition, Type>((v) => v.MessageType).AsReadOnly();
            Validator = new ProtocolValidator(MessageDefinitions.Values.ToArray());
        }
        /// <summary>
        /// Handles raw binary messages and calls the corresponding protocol message handler.
        /// </summary>
        /// <param name="data">Read binary data</param>
        /// <param name="args">context of binary message</param>
        /// <param name="conn">ProtocolCOnnection object whose underlying connection received the message.</param>
        internal void MessageHandler(ReadOnlyMemory<byte> data, MessageArgs args, ProtocolConnection conn)
        {
            if (data.Length == 0) return;
            if (!MessageDefinitions.ContainsKey(data.Span[0])) { return; }

            MessageDefinition msg = MessageDefinitions[data.Span[0]];
            object message = Serializations.Deserialize(data.Slice(1), msg.MessageType);
            msg.Handler.DynamicInvoke(message, new MessageInfo(conn));
        }
        /// <summary>
        /// Serializes a given message and sends it to the protocol connection.
        /// </summary>
        /// <typeparam name="T">Type of the message. Needs to be marked with an <see cref="MessageAttribute"/>.</typeparam>
        /// <param name="message">The message object/value.</param>
        /// <param name="conn">The protocol connection to whose end the message should be sent.</param>
        /// <exception cref="ArgumentException">Thrown when <typeparamref name="T"/> is not a known message type.</exception>
        internal void SendMessage<T>(T message, ProtocolConnection conn)
        {
            if (!MessageDefinitionsByType.ContainsKey(typeof(T))) throw new ArgumentException("" + typeof(T) + " is not a known message type.");
            var mEntry = MessageDefinitionsByType[typeof(T)];
            using (MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte(mEntry.ID);
                ms.Write(Serializations.Serialize(message));
                conn.Connection.WriteMessage(ms.ToArray());
            }
        }
    }
}
