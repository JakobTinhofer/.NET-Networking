using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Networking.BasicProtocols.StructSerializer.DefaultSerializers
{
    [KnownSize(4)]
    public class Int32_Serializer : SerializableType<int>
    {
        public override int Deserializer(ReadOnlySpan<byte> b)
        {
            return BinaryPrimitives.ReadInt32LittleEndian(b);
        }

        public override byte[] Serializer(int obj)
        {
            byte[] b = new byte[4];
            BinaryPrimitives.WriteInt32LittleEndian(b, obj);
            return b;
        }
    }
    [KnownSize(4)]
    public class UInt32_Serializer : SerializableType<uint>
    {
        public override uint Deserializer(ReadOnlySpan<byte> b)
        {
            return BinaryPrimitives.ReadUInt32LittleEndian(b);
        }

        public override byte[] Serializer(uint s)
        {
            byte[] b = new byte[4];
            BinaryPrimitives.WriteUInt32LittleEndian(b, s);
            return b;
        }
    }
    [KnownSize(1)]
    public class Bool_Serializer : SerializableType<bool>
    {
        public override bool Deserializer(ReadOnlySpan<byte> s)
        {
            return s[0] == 0;
        }

        public override byte[] Serializer(bool b)
        {
            return new byte[1] { (byte)(b ? 0 : 1) };
        }
    }

    public class String_Serializer : SerializableType<string>
    {
        public override string Deserializer(ReadOnlySpan<byte> s)
        {
            return Encoding.UTF8.GetString(s);
        }

        public override byte[] Serializer(string obj)
        {
            return Encoding.UTF8.GetBytes(obj);
        }
    }

    [KnownSize(sizeof(double))]
    public class Double_Serializer : SerializableType<double>
    {
        public override double Deserializer(ReadOnlySpan<byte> s)
        {
            return BinaryPrimitives.ReadDoubleLittleEndian(s);
        }

        public override byte[] Serializer(double obj)
        {
            byte[] b = new byte[sizeof(double)];
            BinaryPrimitives.WriteDoubleLittleEndian(b, obj);
            return b;
        }
    }
}
