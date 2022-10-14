using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking.P2P
{
    public class P2PConnector
    {
        public NetworkConnection CoordinatorConnection { get; private set; }

        private readonly int connectionID;
        private readonly bool acceptMultiple;

        public P2PConnector(NetworkConnection coordinator, int connectionID, bool acceptMultiple)
        {
            coordinator.PacketHandler = HandleCoordinatorMessage;
            CoordinatorConnection = coordinator;
            this.connectionID = connectionID;
            this.acceptMultiple = acceptMultiple;
        }


        private void HandleCoordinatorMessage(ReadOnlySpan<byte> packet, PacketArgs args)
        {
            switch (packet[0])
            {
                case 4:
                    var init = new P2PConnectionInitializer(packet.Slice(1, packet.Length - 1));
                    InitiateP2PConnection(init, CoordinatorConnection);
                    break;
                default:
                    break;
            }
        }

        private TaskCompletionSource<NetworkConnection> TCS = new TaskCompletionSource<NetworkConnection>();
        public Task<NetworkConnection> EstablishP2PConnection()
        {
            if (CoordinatorConnection.IsClosed) throw new InvalidOperationException("The coordinator connection has already been closed. Maybe you already got a connection from this connector? (Set acceptMultiple to true if you want to get multiple connections)");
            

            P2PRequest r = new P2PRequest()
            CoordinatorConnection.WritePacket

            return TCS.Task;
        }

        public static NetworkConnection InitiateP2PConnection(P2PConnectionInitializer init, NetworkConnection c)
        {
            throw new NotImplementedException();
        }

        
    }
}
