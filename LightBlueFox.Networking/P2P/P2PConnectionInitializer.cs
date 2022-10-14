using System.Buffers.Binary;
using System.Net;

namespace LightBlueFox.Networking.P2P
{
    /// <summary>
    /// Includes all the information a client needs to try and establish a p2p connection to another client.
    /// </summary>
    public struct P2PConnectionInitializer
    {

        /// <summary>
        /// These are all the endpoints on which the remote client could be reached.
        /// </summary>
        public readonly IPEndPoint[] RemoteEndpoints;

        /// <summary>
        /// The protocol which is expected.
        /// </summary>
        public readonly Protocol Protocol;

        /// <summary>
        /// The id under which the two peers were connected by the coordinator.
        /// </summary>
        public readonly uint ConnectionID;

        /// <summary>
        /// Deserializes the P2P info from the given byte buffer.
        /// </summary>
        public P2PConnectionInitializer(ReadOnlySpan<byte> packet)
        {
            Protocol = (Protocol)packet[0];

            ConnectionID = BinaryPrimitives.ReadUInt32LittleEndian(packet.Slice(1, 4));
            RemoteEndpoints = P2PHelpers.ToIPEPs(packet.Slice(6), packet[5]);


           

        }

        /// <summary>
        /// Create a new Initializer with the given information.
        /// </summary>
        /// <param name="p"></param>
        /// <param name="eps"></param>
        public P2PConnectionInitializer(Protocol p, uint conn_id, params IPEndPoint[] eps)
        {
            RemoteEndpoints = eps;
            Protocol = p;
            ConnectionID = conn_id;
        }

        /// <summary>
        /// Provides a system-independent way of converting this structure to bytes for transfer over the internet.
        /// </summary>
        public byte[] Serialize()
        {
            using(MemoryStream ms = new MemoryStream())
            {
                ms.WriteByte((byte)Protocol);
                Span<byte> conn_id_span = new byte[4];
                BinaryPrimitives.WriteUInt32LittleEndian(conn_id_span, ConnectionID);
                ms.Write(conn_id_span);
                ms.Write(P2PHelpers.WriteIPEPs(RemoteEndpoints));
                return ms.ToArray();
            }
        }
    }
}
