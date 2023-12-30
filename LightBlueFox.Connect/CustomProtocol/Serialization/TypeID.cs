using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using System.Text;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    /// <summary>
    /// Represents a unique 4-byte fingerprint of any type that should be comparable across platforms and contexts. Is execution stable.
    /// </summary>
    [CompositeSerialize]
    public struct TypeID : IEquatable<TypeID>
    {
        private readonly uint fingerprint;

        public TypeID(Type t)
        {
            if (t.IsPrimitive)
            {
                fingerprint = _get4ByteHash(Encoding.Unicode.GetBytes(t.Name));
                return;
            }

            if (t.Namespace == null) throw new ArgumentException("Type has no namespace!");
            if (t.Assembly.FullName == null) throw new ArgumentException("Type assembly name is null!");

            fingerprint = _get4ByteHash(Encoding.Unicode.GetBytes(
                t.Name +
                t.Namespace +
                t.Assembly.FullName +
                t.GetMembers().Length +
                t.GetMethods().Length +
                t.GetFields().Length
                ));
        }


        // As seen in: https://stackoverflow.com/questions/548158/fixed-length-numeric-hash-code-from-variable-length-string-in-c-sharp
        private static uint _get4ByteHash(byte[] arr)
        {
            uint hash = 0;

            foreach (byte b in arr)
            {
                hash += b;
                hash += hash << 10;
                hash ^= hash >> 6;
            }

            hash += hash << 3;
            hash ^= hash >> 11;
            hash += hash << 15;

            return hash;
        }

        public bool Equals(TypeID other)
        {
            return other.fingerprint == fingerprint;
        }

        public static implicit operator byte[](TypeID t)
        {
            return DefaultSerializers.UInt32_Serialize(t.fingerprint);
        }
    }
}
