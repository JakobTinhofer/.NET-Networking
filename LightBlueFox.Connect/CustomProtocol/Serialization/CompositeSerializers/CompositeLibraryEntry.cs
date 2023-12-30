using LightBlueFox.Connect.Util;
using System.Reflection;
using System.Reflection.Emit;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    /// <summary>
    /// A <see cref="SerializationLibraryEntry"/> for a type. Creates custom CIL methods for serialization and deserialization.
    /// </summary>
    /// <typeparam name="T">Any type marked with the <see cref="CompositeSerializeAttribute"/> or deriving attributes.</typeparam>
    public class CompositeLibraryEntry<T> : SerializationLibraryEntry<T>
    {
        private struct FieldArgs { public FieldInfo field; public SerializationLibraryEntry entry; }

        private static SerializerDelegate<T> buildSerializer(List<FieldArgs> fields, SerializationLibrary l, int? size)
        {
            // PREP: Get references to often used methods for use in the CIL code below.
            var uintSer = typeof(DefaultSerializers).GetMethod("UInt32_Serialize", new Type[1] { typeof(uint) }) ?? throw new InvalidOperationException("Could not find uint converter...");
            var arrConv = typeof(ReadOnlySpan<byte>).GetMethod("op_Implicit", new Type[1] { typeof(byte[]) }) ?? throw new Exception("Failed to get operator");
            var msWrite = typeof(MemoryStream).GetMethod("Write", new Type[1] { typeof(ReadOnlySpan<byte>) }) ?? throw new Exception("Failed to get Write");
            var msConstr = typeof(MemoryStream).GetConstructor(new Type[0]) ?? throw new InvalidOperationException("Could not get MemoryStream constructor...");

            Delegate[] serializerParams = new Delegate[fields.Count]; // An array that will be passed to the complete CIL function for it to look up Serilizer delegates for the field types.
                                                                      // TODO: This feels like a bit of a cheap fix; still in search for a more elegant solution.

            DynamicMethod m = new DynamicMethod("EMITD_compser_" + typeof(T), typeof(byte[]), new Type[2] { typeof(T), typeof(Delegate[]) }, true);
            var il = m.GetILGenerator();

            #region [CIL] EMITD_compser_<T>
            var ms = il.DeclareLocal(typeof(MemoryStream));
            var buffer = il.DeclareLocal(typeof(byte[]));

            il.Emit(OpCodes.Newobj, msConstr); // MemoryStream ms = new MemoryStream()
            il.Emit(OpCodes.Stloc, ms);                                                                                                                                //
            

            for (int fi = 0; fi < fields.Count; fi++)
            {
                // PREP
                FieldInfo f = fields[fi].field;
                SerializationLibraryEntry entr = fields[fi].entry;

                var delType = typeof(SerializerDelegate<>).MakeGenericType(f.FieldType);
                serializerParams[fi] = entr.SerializerPointer;
                
                // <--- READ FIELD -->
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, fi);
                il.Emit(OpCodes.Ldelem, typeof(Delegate));


                il.Emit(OpCodes.Ldarg_0); //  Puts the object thats being serialized on the eval stack
                il.Emit(OpCodes.Ldfld, f); // Push the field value onto the stack
                // <------ / -------> STATE:  Stack (top to bottom) FIELDVAL | SERIALIZER

                il.Emit(OpCodes.Callvirt, 
                    delType.GetMethod("Invoke") ?? throw new InvalidOperationException("Could not get invoke method on delegate type..."));
                il.Emit(OpCodes.Stloc, buffer);
                
                // Write entry length as prefix if not fixed
                if (!entr.IsFixedSize)
                {
                    il.Emit(OpCodes.Ldloc, ms); 
                    il.Emit(OpCodes.Ldloc, buffer); 

                    il.Emit(OpCodes.Ldlen); 
                    il.Emit(OpCodes.Call, uintSer); 

                    il.Emit(OpCodes.Call, arrConv); // Convert to span for memory stream write
                    il.Emit(OpCodes.Callvirt, msWrite);
                }

                il.Emit(OpCodes.Ldloc, ms);
                il.Emit(OpCodes.Ldloc, buffer);
                il.Emit(OpCodes.Call, arrConv); // Convert to span for memory stream write
                il.Emit(OpCodes.Callvirt, msWrite); 

            }

            il.Emit(OpCodes.Ldloc, ms);
            il.Emit(OpCodes.Callvirt, typeof(MemoryStream).GetMethod("ToArray") ?? throw new InvalidOperationException("Could not find MemoryStream.ToArray()"));
            il.Emit(OpCodes.Ret);
            #endregion

            var del = (Func<T, Delegate[], byte[]>)m.CreateDelegate(typeof(Func<T, Delegate[], byte[]>));
            return (ob) =>
            {
                byte[] res = del(ob, serializerParams);
                if (size != null && res.Length != size) throw new InvalidOperationException("Created buffer was of size " + res.Length + " not the expected " + size);
                return res;
            };
        }

        private static DeserializerDelegate<T> buildDefaultDeserializer(List<FieldArgs> fields, SerializationLibrary l, int? size)
        {
            // PREP: Get references to often used methods for use in the CIL code below.
            var uintDes = typeof(DefaultSerializers).GetMethod("UInt32_Deserialize", new Type[1] { typeof(ReadOnlyMemory<byte>) }) ?? throw new InvalidOperationException("Could not find uint converter");
            var activator = typeof(Activator).GetMethod("CreateInstance", new Type[1] { typeof(Type) }) ?? throw new("Could not get activator method");
            
            Delegate[] deserializerParams = new Delegate[fields.Count]; // The deserilizers passed to the build CIL method. TODO: more elegant solution
            
            DynamicMethod m = new DynamicMethod("EMITD_compdeser_" + typeof(T), typeof(T), new Type[3] { typeof(ReadOnlyMemory<byte>), typeof(Delegate[]), typeof(Type) }, true);
            var il = m.GetILGenerator();

            #region [CIL] EMITD_compdeser_<T> 
            var bufferIndex = il.DeclareLocal(typeof(int));
            var obj = il.DeclareLocal(typeof(T));
            var len = il.DeclareLocal(typeof(uint));

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, bufferIndex);

            // Creates a new instance of the T type, with all fields uninitialized.
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, activator);
            il.Emit(OpCodes.Stloc, obj);

            for (int i = 0; i < fields.Count; i++)
            {
                FieldInfo field = fields[i].field;
                SerializationLibraryEntry entry = fields[i].entry;

                var delType = typeof(DeserializerDelegate<>).MakeGenericType(field.FieldType);
                deserializerParams[i] = entry.DeserializerPointer;

                // Value types need to be loaded by address.
                if (typeof(T).IsValueType) il.Emit(OpCodes.Ldloca_S, obj);
                else il.Emit(OpCodes.Ldloc, obj);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem, typeof(Delegate));

                // Get field bytes by getting the lenght either from the prefix or the definition if fixedsize.
                if (!entry.IsFixedSize)
                {
                    il.DoSlice(bufferIndex, intLen: sizeof(uint));
                    il.Emit(OpCodes.Call, uintDes);
                    il.Emit(OpCodes.Stloc, len);

                    il.DoSlice(bufferIndex, len, null);
                }
                else
                {
                    il.DoSlice(bufferIndex, intLen: entry.FixedSize ?? throw new("Attribute says fixed size but fixed size is null!"));
                }

                // Call deserializer and store return value at the field in the object shell
                il.Emit(OpCodes.Callvirt, delType.GetMethod("Invoke") ?? throw new("No invoke!"));
                il.Emit(OpCodes.Stfld, field);
            }
            il.Emit(OpCodes.Ldloc, obj);
            il.Emit(OpCodes.Ret);
            #endregion

            var del = m.CreateDelegate<Func<ReadOnlyMemory<byte>, Delegate[], Type, T>>();
            return (m) =>
            {
                if (size != null && m.Length != size) throw new ArgumentException("Need buffer of len " + size + ", only got len " + m.Length);
                return del(m, deserializerParams, typeof(T));
            };
        }

        /// <summary>
        /// Discoveres the fields of T and passes them as well as the corresponding <see cref="SerializationLibraryEntry"/>(s) to the build methods.
        /// </summary>
        private static (int?, SerializerDelegate<T>, DeserializerDelegate<T>) buildSerializationMethods(Type t, SerializationLibrary l)
        {
            if (t.IsArray || t.IsAbstract || t.IsEnum) throw new InvalidOperationException("Cannot be used on arrays/abstract/enum types");

            List<FieldArgs> args = new List<FieldArgs>();
            int? fixedSize = 0;

            // This will retrieve all fields no matter the visibility.
            foreach (var field in RuntimeReflectionExtensions.GetRuntimeFields(typeof(T)))
            {
                if (field.IsStatic) continue;
                try
                {
                    SerializationLibraryEntry entr = l.GetEntry(field.FieldType);
                    args.Add(new()
                    {
                        field = field,
                        entry = entr,
                    });
                    if (fixedSize != null) fixedSize = entr.IsFixedSize ? fixedSize + entr.FixedSize : null;

                }
                catch (SerializationEntryNotFoundException)
                {
                    throw new MissingSerializationDependencyException(field.FieldType);
                }
            }

            return (fixedSize, buildSerializer(args, l, fixedSize), buildDefaultDeserializer(args, l, fixedSize));
        }

        private CompositeLibraryEntry((int?, SerializerDelegate<T>, DeserializerDelegate<T>) args) : base(args.Item1, args.Item2, args.Item3) { }

        /// <summary>
        /// Create a new entry for <typeparamref name="T"/>.
        /// </summary>
        /// <param name="l">Provides the methods for serializing the fields of <typeparamref name="T"/>.</param>
        public CompositeLibraryEntry(SerializationLibrary l) : this(buildSerializationMethods(typeof(T), l)) { }
    }
}
