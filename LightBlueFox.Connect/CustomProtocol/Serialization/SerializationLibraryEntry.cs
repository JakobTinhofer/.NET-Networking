using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    public delegate byte[] SerializerDelegate<T>(T obj);
    public delegate T DeserializerDelegate<T>(ReadOnlyMemory<byte> data);


    public abstract class SerializationLibraryEntry
    {
        public bool IsFixedSize { get { return FixedSize != null; } }
        public int? FixedSize { get; private init; }

        public readonly Type Type;

        public abstract Delegate SerializerPointer { get; }
        public abstract Delegate DeserializerPointer { get; }
        public SerializationLibraryEntry(int? fixedSize, Type type)
        {
            FixedSize = fixedSize;
            Type = type;
        }

        public static SerializationLibraryEntry CreateEntry(SerializationAttribute attr, MethodInfo serializer, MethodInfo deserializer)
        {
            if (!serializer.IsStatic || !deserializer.IsStatic) throw new ArgumentException("Serializer and Deserializer need to be static!");

            Type tSer = typeof(SerializerDelegate<>).MakeGenericType(attr.Type);
            Type tDes = typeof(DeserializerDelegate<>).MakeGenericType(attr.Type);
            Type entryType = typeof(SerializationLibraryEntry<>).MakeGenericType(attr.Type);

            return entryType.GetConstructor(new Type[3] { typeof(int?), tSer, tDes })?.Invoke(new object?[3] { attr.FixedSize, serializer.CreateDelegate(tSer), deserializer.CreateDelegate(tDes) }) as SerializationLibraryEntry ?? throw new InvalidOperationException("Somehow null in creating entry");
        }

        public static SerializationLibraryEntry CreateArrayEntry(SerializationLibraryEntry baseType)
        {
            return typeof(ArraySerializationLibraryEntry<>)
                .MakeGenericType(baseType.Type)
                .GetConstructor(new Type[1]{ typeof(SerializationLibraryEntry<>).MakeGenericType(baseType.Type) })
                ?.Invoke(new object[1] {baseType})
                as SerializationLibraryEntry ?? throw new("Could not create Array entry");
        }
    }

    public class SerializationLibraryEntry<T> : SerializationLibraryEntry
    {
        private readonly SerializerDelegate<T> Serializer;
        private readonly DeserializerDelegate<T> Deserializer;

        public override Delegate SerializerPointer => Serializer;

        public override Delegate DeserializerPointer => Deserializer;

        public SerializationLibraryEntry(int? fixedSize, SerializerDelegate<T> ser, DeserializerDelegate<T> des) : base(fixedSize, typeof(T)){
            Serializer = ser;
            Deserializer = des;
        }

        public SerializationLibraryEntry(int? fixedSize, (SerializerDelegate<T>, DeserializerDelegate<T>) ser) : this(fixedSize, ser.Item1, ser.Item2) { }

        public T Deserialize(ReadOnlyMemory<byte> data) { return Deserializer(data); }
        public byte[] Serialize(T obj) { return Serializer(obj); }
    }
}
