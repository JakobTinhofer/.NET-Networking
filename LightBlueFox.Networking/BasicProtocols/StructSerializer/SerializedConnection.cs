using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking.BasicProtocols.StructSerializer
{
    public class SerializedConnection<T> where T : Connection
    {
        public readonly T Connection;

        public readonly SerializationLibrary Library;

        public SerializedConnection(T con){
            Connection = con;
        }

        private Handle
    }
}
