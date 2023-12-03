using LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers;
using System.ComponentModel.Design;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    public class SerializationEntryNotFoundException : Exception
    {
        public readonly Type EntryType;
        public SerializationEntryNotFoundException(Type t) : base("No serializer for " + t + " found.")
        {
            EntryType = t;
        }
    }

    public class SerializationLibrary
    {
        internal SerializationLibraryEntry<T> GetEntry<T>()
        {
            return (( GetEntry(typeof(T)) as SerializationLibraryEntry<T>) ?? throw new InvalidOperationException("Somehow got null after entry conversion"));
        }

        internal SerializationLibraryEntry GetEntry(Type t)
        {
            if (!SavedSerialization.ContainsKey(t))
            {
                if (t.IsArray) SavedSerialization[t] = SerializationLibraryEntry.CreateArrayEntry(GetEntry(t.GetElementType() ?? throw new("Could not get array element type")));
                else throw new SerializationEntryNotFoundException(t);
            }
            return SavedSerialization[t];
        }

        public T Deserialize<T>(ReadOnlyMemory<byte> data)
        {
            return GetEntry<T>().Deserialize(data);
        }

        public byte[] Serialize<T>(T obj)
        {
            return GetEntry<T>().Serialize(obj);
        }

        private readonly Dictionary<Type, SerializationLibraryEntry> SavedSerialization = new();

        public SerializationLibrary()
        {
            AddSerializers(typeof(DefaultSerializers));
        }

        private const int MAXRECURSION = 5;
        private void _recurseAddCompositeSerializares(Dictionary<Type, Type> cyclicCheck, List<Type> compositeTypes, int lvl)
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
                    var t = typeof(CompositeLibraryEntry<>).MakeGenericType(ct).GetConstructor(new[] { typeof(SerializationLibrary) }) ?? throw new InvalidOperationException("Could not find constructor on CompositeBlueprint!");
                    SavedSerialization.Add(ct, (SerializationLibraryEntry)t.Invoke(new object[] { this }));
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

            if (staggeredTypes.Count > 0) { _recurseAddCompositeSerializares(cyclicCheck, staggeredTypes, lvl); }
        }

        public void AddCompositeSerializers(params Type[] compositeTypes)
        {
            _recurseAddCompositeSerializares(new(), new(compositeTypes), 0);
        }

        public void AddSerializers(params Type[] types)
        {
            Dictionary<Type, (SerializationAttribute, MethodInfo)> serializers = new();
            Dictionary<Type, (SerializationAttribute, MethodInfo)> deserializers = new();

            List<Type> compositeTypes = new List<Type>();


            // COOOOL!!!!!!! 4 NESTED LOOOPS!!!!!!
            foreach (var t in types)
            {

                List<Type> nestedTypes = new() { t };
                List<Type> nextNestedTypes = new();
                while(nestedTypes.Count > 0)
                {
                    foreach (var nt in nestedTypes)
                    {
                        var compAttr = nt.GetCustomAttribute<CompositeSerializeAttribute>();
                        
                        if (compAttr != null)
                        {
                            compositeTypes.Add(nt);
                        }

                        foreach (var m in t.GetMethods())
                        {
                            var attr = m.GetCustomAttribute<SerializationMethodAttribute>();
                            
                            if (!(m.IsStatic && attr != null)) { continue; }

                            Type serType = attr.CheckSerializerType(m);
                            Dictionary<Type, (SerializationAttribute, MethodInfo)> dict = attr.IsSerializer ? serializers : deserializers;



                            if (SavedSerialization.ContainsKey(serType)) throw new NotImplementedException("Multiple serializers for one type not implemented.");
                            if (dict.ContainsKey(serType)) throw new NotImplementedException("Multiple serializers for one type not implemented.");
                            dict.Add(serType, (attr, m));
                        }

                        nextNestedTypes.AddRange(nt.GetNestedTypes());
                    }
                    nestedTypes = nextNestedTypes;
                    nextNestedTypes = new ();
                }                
            }

            foreach (var t in (serializers.Count > deserializers.Count ? serializers : deserializers).Keys)
            {
                if (!deserializers.ContainsKey(t)) throw new ArgumentException("Can only add serializers and deserializers in pairs!");
                if (serializers[t].Item1.FixedSize != deserializers[t].Item1.FixedSize) throw new ArgumentException("Conflicting size instructions!");
                SavedSerialization.Add(t, SerializationLibraryEntry.CreateEntry(deserializers[t].Item1, t, serializers[t].Item2, deserializers[t].Item2));
            }

            if(compositeTypes.Count > 0) AddCompositeSerializers(compositeTypes.ToArray());
            
        }
    }
}
