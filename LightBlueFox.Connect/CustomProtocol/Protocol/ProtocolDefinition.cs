using LightBlueFox.Connect.CustomProtocol.Serialization;
using LightBlueFox.Connect.Util;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    public delegate void HandleMessageAnswer<T>(T ans);
    public delegate void HandleCustomError(Exception ex);

    public class ProtocolDefinition
    {
        private readonly SerializationLibrary Serializations;

        private readonly IReadOnlyDictionary<Type, MessageDefinition> MessageDefinitionsByType = new Dictionary<Type,MessageDefinition>();
        
        private readonly Dictionary<byte, MessageDefinition> MessageDefinitions = new();

        public readonly ProtocolValidator Validator;

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
        public ProtocolDefinition(SerializationLibrary sl, params Type[] t)
        {
            Serializations = sl;
            ReadDefinitions(t.Append(typeof(DefaultMessages)).ToArray());
            MessageDefinitionsByType = MessageDefinitions.Values.ToDictionary<MessageDefinition, Type>((v) => v.MessageType).AsReadOnly();
            Validator = new ProtocolValidator(MessageDefinitions.Values.ToArray());
        }
        internal void MessageHandler(ReadOnlyMemory<byte> data, MessageArgs args, ProtocolConnection conn)
        {
            if (data.Length == 0) return;
            if (!MessageDefinitions.ContainsKey(data.Span[0])) { return; }

            MessageDefinition msg = MessageDefinitions[data.Span[0]];
            object message = Serializations.Deserialize(data.Slice(1), msg.MessageType);
            msg.Handler.DynamicInvoke(message, new MessageInfo(conn));
        }
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
