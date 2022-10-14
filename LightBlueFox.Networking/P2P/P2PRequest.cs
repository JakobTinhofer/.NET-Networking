using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking.P2P
{
    internal struct P2PRequest
    {
        /// <summary>
        /// The id under which the coordinator should connect the two clients.
        /// </summary>
        public readonly uint ConnectionID;

        /// <summary>
        /// All known private EPs.
        /// </summary>
        public readonly IPEndPoint[] RemoteEndpoints;

        /// <summary>
        /// The prefered protocol.
        /// </summary>
        public readonly Protocol Protocol;

        public P2PRequest(uint ConnectionID)
    }
}
