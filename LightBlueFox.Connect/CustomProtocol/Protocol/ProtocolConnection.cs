using LightBlueFox.Connect.Structure;

namespace LightBlueFox.Connect.CustomProtocol.Protocol
{
    /// <summary>
    /// Wraps any kind of <see cref="Connection"/>, then used to send and receive message objects that are defined in the underlying protocol.
    /// </summary>
    public class ProtocolConnection
    {
        /// <summary>
        /// The underlying physical connection.
        /// </summary>
        public readonly Connection Connection;

        /// <summary>
        /// The protocol definition in use.
        /// </summary>
        public readonly ProtocolDefinition Protocol;

        /// <summary>
        /// Creates a new ProtocolConnection from an established connection. This will check whether both ends agree on the protocol definition first and fail if they dont.
        /// </summary>
        /// <param name="def">The shared protocol definition. Needs to be compatible with the other end.</param>
        /// <param name="con">Any already established connection.</param>
        /// <param name="pos">From what position to negotiate. Needs to be the opposite on the other end.</param>
        /// <returns>A validated, ready-to-go protocol connection.</returns>
        public static ProtocolConnection CreateWithValidation(ProtocolDefinition def, Connection con, ConnectionNegotiationPosition pos)
        {
            ConnectionNegotiation.ValidateConnection(con, pos, def.Validator);
            return new ProtocolConnection(def, con);
        }

        /// <summary>
        /// Only use when protocol validation has already occured.
        /// </summary>
        internal static ProtocolConnection CreateWithoutValidation(ProtocolDefinition def, Connection con)
        {
            return new ProtocolConnection(def, con);
        }

        private ProtocolConnection(ProtocolDefinition prot, Connection con)
        {
            Connection = con;
            Protocol = prot;
            Connection.MessageHandler = (d, arg) => prot.MessageHandler(d, arg, this);
        }

        /// <summary>
        /// Write a new message to the other end.
        /// </summary>
        /// <typeparam name="T">Any Message type. Needs to have the <see cref="MessageAttribute"/>.</typeparam>
        /// <param name="message">The message to send</param>
        public void WriteMessage<T>(T message)
        {
            Protocol.SendMessage(message, this);
        }
    }
}
