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
        public readonly SerializationAttribute Attribute;

        public abstract Delegate SerializerPointer { get; }
        public abstract Delegate DeserializerPointer { get; }


        public SerializationLibraryEntry(SerializationAttribute attr)
        {
            Attribute = attr;
        }

        public static SerializationLibraryEntry CreateEntry(SerializationAttribute attr, MethodInfo serializer, MethodInfo deserializer)
        {
            if (!serializer.IsStatic || !deserializer.IsStatic) throw new ArgumentException("Serializer and Deserializer need to be static!");

            Type tSer = typeof(SerializerDelegate<>).MakeGenericType(attr.Type);
            Type tDes = typeof(DeserializerDelegate<>).MakeGenericType(attr.Type);
            Type entryType = typeof(SerializationLibraryEntry<>).MakeGenericType(attr.Type);

            return entryType.GetConstructor(new Type[3] { typeof(SerializationAttribute), tSer, tDes })?.Invoke(new object[3] { attr, serializer.CreateDelegate(tSer), deserializer.CreateDelegate(tDes) }) as SerializationLibraryEntry ?? throw new InvalidOperationException("Somehow null in creating entry");
        }

        public static SerializationLibraryEntry CreateArrayEntry(SerializationLibraryEntry baseType)
        {
            return typeof(ArraySerializationLibraryEntry<>)
                .MakeGenericType(baseType.Attribute.Type)
                .GetConstructor(new Type[1]{ typeof(SerializationLibraryEntry<>).MakeGenericType(baseType.Attribute.Type) })
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

        public SerializationLibraryEntry(SerializationAttribute attr, SerializerDelegate<T> ser, DeserializerDelegate<T> des) : base(attr){
            Serializer = ser;
            Deserializer = des;
        }

        public SerializationLibraryEntry(SerializationAttribute attr, (SerializerDelegate<T>, DeserializerDelegate<T>) ser) : this(attr, ser.Item1, ser.Item2) { }




        public T Deserialize(ReadOnlyMemory<byte> data) { return Deserializer(data); }
        public byte[] Serialize(T obj) { return Serializer(obj); }

        
    }
}
