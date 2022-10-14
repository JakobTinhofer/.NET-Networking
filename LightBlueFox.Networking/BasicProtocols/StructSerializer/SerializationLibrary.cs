using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using LightBlueFox.Networking.BasicProtocols.StructSerializer.DefaultSerializers;

namespace LightBlueFox.Networking.BasicProtocols.StructSerializer
{
    public static class SerializationLibrary
    {
        private static Dictionary<Type, SerializableTypeHolder> Library = new Dictionary<Type, SerializableTypeHolder>() {
            { typeof(int), wrap_type(new Int32_Serializer()) },
            { typeof(uint), wrap_type(new UInt32_Serializer()) },
            { typeof(bool), wrap_type(new Bool_Serializer()) },
            { typeof(string), wrap_type(new String_Serializer()) },
            { typeof(double), wrap_type(new Double_Serializer()) },
        };

        public static SerializableType<T> GetSerializer<T>(){
            return (SerializableType<T>)GetSerializer(typeof(T));
        }
        public static SerializableType GetSerializer(Type type)
        {
            return Library[type].Type;
        }

        private class SerializableTypeHolder
        {
            public readonly SerializableType Type;
            public readonly int? KnownSize;

            public SerializableTypeHolder(SerializableType t, int? knownSize)
            {
                Type = t;
                KnownSize = knownSize;
            }
        }
        public static byte[] SerializeAll(Type[] t, object[] obj)
        {
            if (t.Length == 0 || obj.Length != t.Length) throw new ArgumentException("Both arrays need to be the same non zero length!");
            using (MemoryStream ms = new MemoryStream())
            {
                for (int i = 0; i < t.Length; i++)
                {
                    if (!Library.ContainsKey(t[i])) throw new KeyNotFoundException("Unknown type in type array.");
                    var l = Library[t[i]];
                    if (l.KnownSize != null)
                        ms.Write(Serialize(l.KnownSize));
                    var method = (typeof(SerializationLibrary)
                        .GetMethod("Serialize") ?? throw new InvalidOperationException())
                        .MakeGenericMethod(t[i]);
                    ms.Write((ReadOnlySpan<byte>)method.Invoke(null, new object[1] { Convert.ChangeType(obj[i], t[i])});
                }
            }
            
            
        }

        public static byte[] Serialize<T>(T obj)
        {
            if (typeof(T).IsArray)
            {

                var method = (typeof(SerializationLibrary)
                    .GetMethod("SerializeArray") ?? throw new InvalidOperationException("Cannot find array serialization method..."))
                    .MakeGenericMethod(typeof(T).GetElementType() ?? throw new InvalidOperationException("Array of null type!"));
                return method.Invoke(null, new object[1] { obj }) as byte[] ?? throw new InvalidOperationException("Could not automatically call SerializeArray.");
            }
            else
            {

                if (obj == null || !Library.ContainsKey(obj.GetType())) throw new ArgumentException("The given obj was either null or there is no info on how to handle the given type.");
                return (Library[obj.GetType()].Type as SerializableType<T> ?? throw new InvalidOperationException("Invalid entry in serialization library.")).Serializer(obj);

            }
        }
        
        public static byte[] SerializeArray<T>(T[] arr)
        {
            var size = TryGetTypeSize<T>();
            using (MemoryStream ms = new MemoryStream())
            {
                ms.Write(Serialize(arr.Length));
                foreach (var item in arr)
                {
                    var i = Serialize(item);
                    if (size == null) ms.Write(Serialize(i.Length));
                    ms.Write(i);
                }
                return ms.ToArray();
            }
        }

        public static T Deserialize<T>(ReadOnlySpan<byte> b)
        {
            if (typeof(T).IsArray)
            {

                var method = (typeof(SerializationLibrary)
                    .GetMethod("DeserializeArray") ?? throw new InvalidOperationException("Cannot find array serialization method..."))
                    .MakeGenericMethod(typeof(T).GetElementType() ?? throw new InvalidOperationException("Array of null type!"));
                return (T)(method.Invoke(null, new object[1] { b.ToArray() /*  Any way to not copy here? :( */ }) ?? throw new InvalidOperationException("Could not automatically call SerializeArray."));
            }
            else
            {
                if (!Library.ContainsKey(typeof(T))) throw new ArgumentException("There is no info on how to handle the given type.");
                return (Library[typeof(T)].Type as SerializableType<T> ?? throw new InvalidOperationException("Invalid entry in serialization library.")).Deserializer(b);
            }
        }

        public static T[] DeserializeArray<T>(ReadOnlySpan<byte> s)
        {
            var size = TryGetTypeSize<T>();
            var count = Deserialize<int>(s.Slice(4));
            var array = new T[count];
            int span_in = 4;


            ReadOnlySpan<byte> span;
            for (int i = 0; i < count; i++)
            {
                if (size == null)
                {
                    span = s.Slice(span_in + 4, Deserialize<int>(s.Slice(span_in, 4)));
                    span_in += 4;
                }
                else
                {
                    span = s.Slice(span_in, size ?? -1);
                }
                array[i] = Deserialize<T>(span);
                span_in += span.Length;
            }

            return array;
        }

        private static SerializableTypeHolder wrap_type(SerializableType t)
        {
            var attr = t.GetType().GetCustomAttribute<KnownSizeAttribute>();
            return new SerializableTypeHolder(t, attr == null ? null : attr.Size);
        }

        public static void Load(bool overwrite = false, params SerializableType[] types)
        {
            foreach (var item in types)
            {
                if (!Library.ContainsKey(item.GetType()))
                {
                    Library.Add(item.GetType(), wrap_type(item));
                }
                else if(overwrite)
                {
                    Library[item.GetType()] = wrap_type(item);
                }
                else
                {
                    throw new InvalidOperationException("Duplicate entry in serialization library. You can overwrite other entries by setting the overwrite param to true.");
                }
            }
        }
    
        public static int? TryGetTypeSize<T>()
        {
            if (Library.ContainsKey(typeof(T)))
            {
                return Library[typeof(T)].KnownSize;
            }
            throw new ArgumentException("The given type is not present in the serialization library!");
        }
    }

    public class SerializableType
    {

    }

    public abstract class SerializableType<T> : SerializableType
    {
        public abstract T Deserializer(ReadOnlySpan<byte> s);
        public abstract byte[] Serializer(T obj);
    }

}
