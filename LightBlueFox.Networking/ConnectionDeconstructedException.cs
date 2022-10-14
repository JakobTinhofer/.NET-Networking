using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking
{
    public class ConnectionDeconstructedException : Exception
    {
        public ConnectionDeconstructedException(string message = "The socket of this connection has been retrieved and should no longer be accessed.") : base(message)
        {
        }
    }
}
