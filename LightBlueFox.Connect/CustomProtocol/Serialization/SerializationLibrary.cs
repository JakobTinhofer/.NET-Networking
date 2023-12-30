using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using LightBlueFox.Connect.Util;
using System.Reflection;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    /// <summary>
    /// Thrown when a type is not known to the <see cref="SerializationLibrary"/>.
    /// </summary>
    public class SerializationEntryNotFoundException : Exception
    {
        public readonly Type EntryType;
        public SerializationEntryNotFoundException(Type t) : base("No serializer for " + t + " found.")
        {
            EntryType = t;
        }
    }

    /// <summary>
    /// Stores a library of <see cref="SerializationLibraryEntry"/>(s) each allowing for the (de)serialization of a certain type. 
    /// </summary>
    public class SerializationLibrary
    {
        /// <summary>
        /// Returns the <see cref="SerializationLibraryEntry"/> for type <typeparamref name="T"/>, if known.
        /// </summary>
        /// <typeparam name="T">The type to look up in the library.</typeparam>
        /// <returns>The entry for <typeparamref name="T"/>.</returns>
        /// <exception cref="SerializationEntryNotFoundException">The type <typeparamref name="T"/> has not yet been added to the library.</exception>
        public SerializationLibraryEntry<T> GetEntry<T>()
        {
            return (GetEntry(typeof(T)) as SerializationLibraryEntry<T>) ?? throw new InvalidOperationException("Somehow got null after entry conversion");
        }

        /// <summary>
        /// Returns the <see cref="SerializationLibraryEntry"/> for type <paramref name="t"/>, if known.
        /// </summary>
        /// <param name="t">The type to look up in the library.</param>
        /// <returns>The entry for <typeparamref name="T"/>.</returns>
        /// <exception cref="SerializationEntryNotFoundException">The type <paramref name="t"/> has not yet been added to the library.</exception>
        public SerializationLibraryEntry GetEntry(Type t)
        {
            if (!SavedSerialization.ContainsKey(t))
            {
                if (t.IsArray) SavedSerialization[t] = SerializationLibraryEntry.ArrayEntryFactory(GetEntry(t.GetElementType() ?? throw new("Could not get array element type")));
                else if (t.IsEnum) SavedSerialization[t] = SerializationLibraryEntry.CreateEnumEntry(t, this);
                else throw new SerializationEntryNotFoundException(t);
            }
            return SavedSerialization[t];
        }

        /// <summary>
        /// Tries to deserialize the given data to the type <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">The type the data should be deserialized to.</typeparam>
        /// <param name="data">The binary representation of an object/value of type <typeparamref name="T"/></param>
        /// <returns>The deserialized object/value.</returns>
        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            return GetEntry<T>().Deserialize(data);
        }

        /// <summary>
        /// Serializes a given object to its binary representation.
        /// </summary>
        /// <typeparam name="T">The type of the object to serialize.</typeparam>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The binary representation of <paramref name="obj"/>.</returns>
        public byte[] Serialize<T>(T obj)
        {
            return GetEntry<T>().Serialize(obj);
        }

        /// <summary>
        /// Tries to deserialize the given data to the type <paramref name="t"/>.
        /// </summary>
        /// <param name="t">The type the data should be deserialized to.</param>
        /// <param name="data">The binary representation of an object/value of type <paramref name="t"/></param>
        /// <returns>The deserialized object/value.</returns>
        public object Deserialize(ReadOnlyMemory<byte> data, Type t)
        {
            return GetEntry(t).DeserializerPointer.DynamicInvoke(data) ?? throw new InvalidOperationException("Deserializer invoke did not return object!");
        }


        /// <summary>
        /// Serializes a given object to its binary representation.
        /// </summary>
        /// <param name="T">The type of the object to serialize.</param>
        /// <param name="obj">The object to serialize.</param>
        /// <returns>The binary representation of <paramref name="obj"/>.</returns>
        public byte[] Serialize(object o)
        {
            return GetEntry(o.GetType()).SerializerPointer.DynamicInvoke(o) as byte[] ?? throw new InvalidOperationException("Serializer invoke did not return bytes!");
        }

        /// <summary>
        /// Creates a new SerializationLibrary. It will contain only the serializers from <see cref="DefaultSerializers"/> for now.
        /// </summary>
        public SerializationLibrary()
        {
            AddSerializers(typeof(DefaultSerializers));
        }

        /// <summary>
        /// Add all (de)serializers contained in the given types. Will throw an exception if duplicates are detected.
        /// </summary>
        /// <param name="types">The types to crawl for (de)serializers</param>
        /// <returns>A list of the Types found and added to the library.</returns>
        public Type[] AddSerializers(params Type[] types) { return FilterForSerialization(false, null, null, null, types); }

        /// <summary>
        /// Add all (de)serializers contained in the given types. Duplicates are ignored.
        /// </summary>
        /// <param name="types">The types to crawl for (de)serializers</param>
        /// <returns>A list of the Types found and added to the library.</returns>
        public Type[] TryAddSerializers(params Type[] types) { return FilterForSerialization(true, null, null, null, types); }
        
        
        
        /// <summary>
        /// Allows for adding custom additional filters that will be called while crawling the types.
        /// </summary>
        internal Type[] FilterForSerialization(bool ignoreDuplicates = false,
                                        Action<Type>? additionalTypeActions = null,
                                        Action<MethodInfo, Type>? additionalMethodActions = null,
                                        Action<FieldInfo, Type>? additionalFieldActions = null,
                                        params Type[] types)
        {

            List<Type> addedTypes = new();

            Dictionary<Type, (MethodInfo, SerializationMethodAttribute)> serializers = new Dictionary<Type, (MethodInfo, SerializationMethodAttribute)>();
            Dictionary<Type, (MethodInfo, SerializationMethodAttribute)> deserializers = new Dictionary<Type, (MethodInfo, SerializationMethodAttribute)>();

            HashSet<Type> compositeTypes = new();

            Action<Type> tFilter = (t) =>
            {
                var attr = t.GetCustomAttribute<CompositeSerializeAttribute>(true);
                if (!t.IsAbstract && attr != null)
                {
                    compositeTypes.Add(t);
                }

                if (additionalTypeActions != null) additionalTypeActions(t);
            };

            Action<MethodInfo, Type> mFilter = (m, t) =>
            {
                var attrList = m.GetCustomAttributes<SerializationMethodAttribute>(true);
                if (attrList.Count() > 1) throw new InvalidOperationException("Cannot have more than one Atomic method attribute on a single method!");
                var attr = attrList.FirstOrDefault();
                if (m.IsStatic && attr != null)
                {
                    var dict = attr.IsSerializer ? serializers : deserializers;
                    dict.Add(attr.CheckSerializerType(m), (m, attr));
                }
                if (additionalMethodActions != null) additionalMethodActions(m, t);
            };

            SerializationHelpers.ForEachMember(types, mFilter, tFilter, additionalFieldActions);


            var dict = serializers.Count > deserializers.Count ? serializers : deserializers;
            var otherDict = dict == serializers ? deserializers : serializers;
            foreach (var r in dict)
            {
                var ser = r.Value.Item2.IsSerializer;
                var t = r.Key;

                if (SavedSerialization.ContainsKey(t) || addedTypes.Contains(t))
                {
                    if (ignoreDuplicates) continue;
                    else throw new NotImplementedException("Multiple serializers for one type not implemented.");
                }

                if (!otherDict.ContainsKey(t)) throw new ArgumentException("Can only add serializers and deserializers in pairs!");
                if (serializers[t].Item2.FixedSize != deserializers[t].Item2.FixedSize) throw new ArgumentException("Conflicting size instructions!");
                SavedSerialization.Add(t, SerializationLibraryEntry.MethodEntryFactory(deserializers[t].Item2, t, serializers[t].Item1, deserializers[t].Item1));
                addedTypes.Add(t);
            }

            if (compositeTypes.Count > 0) _recurseAddCompositeSerializares(new(), new(compositeTypes), 0, addedTypes, ignoreDuplicates);

            return addedTypes.ToArray();
        }

        private readonly Dictionary<Type, SerializationLibraryEntry> SavedSerialization = new();
        private const int MAXRECURSION = 5;

        private List<Type> _recurseAddCompositeSerializares(Dictionary<Type, Type> cyclicCheck, List<Type> compositeTypes, int lvl, List<Type> addedTypes, bool ignoreDuplicates)
        {
            if (lvl == MAXRECURSION) throw new InvalidOperationException("Reached max recursion limit on trying to avoid dependencies.");
            lvl++;

            List<Type> staggeredTypes = new List<Type>();
            foreach (var ct in compositeTypes)
            {
                var attr = ct.GetCustomAttribute<CompositeSerializeAttribute>();
                if (attr == null) throw new ArgumentException("Given type does not have CompositeSerializeAttribute!");
                try
                {
                    if (SavedSerialization.ContainsKey(ct) || addedTypes.Contains(ct))
                    {
                        if (ignoreDuplicates) continue;
                        else throw new NotImplementedException("Multiple serializers for one type not implemented.");
                    }

                    var t = typeof(CompositeLibraryEntry<>).MakeGenericType(ct).GetConstructor(new[] { typeof(SerializationLibrary) }) ?? throw new InvalidOperationException("Could not find constructor on CompositeBlueprint!");
                    SavedSerialization.Add(ct, (SerializationLibraryEntry)t.Invoke(new object[] { this }));
                    addedTypes.Add(ct);
                }
                catch (TargetInvocationException tEx)
                {
                    var ex = tEx.InnerException as MissingSerializationDependencyException ?? throw tEx.InnerException ?? new Exception("An unknown error occured in creating the custom serializers: " + tEx.Message + " @ " + tEx.Source);

                    if (cyclicCheck.ContainsKey(ex.MissingDependency) && cyclicCheck[ex.MissingDependency] == ct) throw new CyclicDependencyException(ct, ex.MissingDependency);
                    if (!compositeTypes.Contains(ex.MissingDependency)) throw ex;
                    cyclicCheck.Add(ct, ex.MissingDependency);
                    staggeredTypes.Add(ct);
                }
            }
            if (staggeredTypes.Count > 0) { _recurseAddCompositeSerializares(cyclicCheck, staggeredTypes, lvl, addedTypes, ignoreDuplicates); }
            return addedTypes;
        }
    }
}
