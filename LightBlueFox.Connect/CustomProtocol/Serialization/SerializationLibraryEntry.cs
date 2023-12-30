using System.Reflection;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    /// <summary>
    /// Describes a Serializer for type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">The type of the object to serialize.</typeparam>
    /// <param name="obj">The object to serialize</param>
    /// <returns>The binary representation of the obj param.</returns>
    public delegate byte[] SerializerDelegate<T>(T obj);
    /// <summary>
    /// Describes a Deserializer for type <typeparamref name="T"/>
    /// </summary>
    /// <typeparam name="T">Te type of the object to deserialize</typeparam>
    /// <param name="data">The binary representation of the object to deserialize</param>
    /// <returns>The deserialized object of type <typeparamref name="T"/></returns>
    public delegate T DeserializerDelegate<T>(ReadOnlyMemory<byte> data);

    /// <summary>
    /// An entry in the <see cref="SerializationLibrary"/> that contains a Serializer and Deserializer for a certain type.
    /// </summary>
    public abstract class SerializationLibraryEntry
    {
        /// <summary>
        /// Describes whether the serialized data will always be the same length.
        /// </summary>
        public bool IsFixedSize { get { return FixedSize != null; } }
        
        /// <summary>
        /// If <see cref="IsFixedSize"/>, the size of the type.
        /// </summary>
        public int? FixedSize { get; private init; }

        /// <summary>
        /// The type represented by this entry.
        /// </summary>
        public readonly Type Type;

        /// <summary>
        /// A unique 4-byte identifier of <see cref="SerializationLibraryEntry.Type"/> that is platfrom- and context-independent.
        /// </summary>
        public readonly TypeID TypeID;

        /// <summary>
        /// A delegate of the serializer.
        /// </summary>
        public abstract Delegate SerializerPointer { get; }
        /// <summary>
        /// A delegate of the deserializer.
        /// </summary>
        public abstract Delegate DeserializerPointer { get; }

        protected SerializationLibraryEntry(int? fixedSize, Type type)
        {
            FixedSize = fixedSize;
            Type = type;
            TypeID = new TypeID(type);
        }


        /// <summary>
        /// Creates a new <see cref="SerializationLibraryEntry{T}"/> for a type from two serialization methods
        /// </summary>
        internal static SerializationLibraryEntry MethodEntryFactory(SerializationMethodAttribute attr, Type t, MethodInfo serializer, MethodInfo deserializer)
        {
            if (!serializer.IsStatic || !deserializer.IsStatic) throw new ArgumentException("Serializer and Deserializer need to be static!");

            Type tSer = typeof(SerializerDelegate<>).MakeGenericType(t);
            Type tDes = typeof(DeserializerDelegate<>).MakeGenericType(t);
            Type entryType = typeof(SerializationLibraryEntry<>).MakeGenericType(t);

            return entryType.GetConstructor(new Type[3] { typeof(int?), tSer, tDes })?.Invoke(new object?[3] { attr.FixedSize, serializer.CreateDelegate(tSer), deserializer.CreateDelegate(tDes) }) as SerializationLibraryEntry ?? throw new InvalidOperationException("Somehow null in creating entry");
        }

        /// <summary>
        /// Creates a new <see cref="ArraySerializationLibraryEntry{T}"/> for the given base type.
        /// </summary>
        /// <param name="baseType">The type that the array is based on.</param>
        internal static SerializationLibraryEntry ArrayEntryFactory(SerializationLibraryEntry baseType)
        {
            return typeof(ArraySerializationLibraryEntry<>)
                .MakeGenericType(baseType.Type)
                .GetConstructor(new Type[1] { typeof(SerializationLibraryEntry<>).MakeGenericType(baseType.Type) })
                ?.Invoke(new object[1] { baseType })
                as SerializationLibraryEntry ?? throw new("Could not create Array entry");
        }

        /// <summary>
        /// Creates a new entry for a given enum type.
        /// </summary>
        /// <param name="t">The type of the enum</param>
        /// <param name="l">The serialization library that allows for the (de)serialization of the enum's underlying type.</param>
        internal static SerializationLibraryEntry CreateEnumEntry(Type t, SerializationLibrary l)
        {
            if (!t.IsEnum) throw new ArgumentException("T needs to be an enum");
            return (SerializationLibraryEntry)(typeof(EnumSerializationEntry<>).MakeGenericType(t).GetConstructor(new Type[1] { typeof(SerializationLibrary) })?.Invoke(new[] { l }) ?? throw new NullReferenceException("Got null while creating enum serializer."));
        }
    }

    /// <summary>
    /// An entry in the <see cref="SerializationLibrary"/> for
    /// the type <typeparamref name="T"/> that contains a <see cref="SerializerDelegate{T}"/> 
    /// and <see cref="DeserializerDelegate{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type represented by this entry.</typeparam>
    public class SerializationLibraryEntry<T> : SerializationLibraryEntry
    {
        private readonly SerializerDelegate<T> Serializer;
        private readonly DeserializerDelegate<T> Deserializer;
        public override Delegate SerializerPointer => Serializer;
        public override Delegate DeserializerPointer => Deserializer;

        public SerializationLibraryEntry(int? fixedSize, SerializerDelegate<T> ser, DeserializerDelegate<T> des) : base(fixedSize, typeof(T))
        {
            Serializer = ser;
            Deserializer = des;
        }
        public SerializationLibraryEntry(int? fixedSize, (SerializerDelegate<T>, DeserializerDelegate<T>) ser) : this(fixedSize, ser.Item1, ser.Item2) { }

        public T Deserialize(ReadOnlyMemory<byte> data) { return Deserializer(data); }
        public byte[] Serialize(T obj) { return Serializer(obj); }
    }
}
