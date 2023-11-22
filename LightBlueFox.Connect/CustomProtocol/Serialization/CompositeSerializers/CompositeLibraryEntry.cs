using LightBlueFox.Connect.Util;
using System.Data;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    public class CompositeLibraryEntry<T> : SerializationLibraryEntry<T>
    {
        public static bool AutoSerializePrivateFields = true;


        private struct FieldArgs { public FieldInfo field; public SerializationLibraryEntry entry; }

        private static SerializerDelegate<T> buildSerializer(List<FieldArgs> fields, SerializationLibrary l, int? size)
        {
            // PREP: 
            var uintSer = typeof(DefaultSerializers).GetMethod("UInt32_Serialize", new Type[1] { typeof(uint) }) ?? throw new InvalidOperationException("Could not find uint converter...");
            var arrConv = typeof(ReadOnlySpan<byte>).GetMethod("op_Implicit", new Type[1] { typeof(byte[]) }) ?? throw new Exception("Failed to get operator");
            var msWrite = typeof(MemoryStream).GetMethod("Write", new Type[1] { typeof(ReadOnlySpan<byte>) }) ?? throw new Exception("Failed to get Write");

            Delegate[] paramArray = new Delegate[fields.Count];



            DynamicMethod m = new DynamicMethod("EMITD_compser_" + typeof(T), typeof(byte[]), new Type[2] { typeof(T), typeof(Delegate[]) }, true);
            var il = m.GetILGenerator();

            #region CIL

            #region Setup
            il.DeclareLocal(typeof(MemoryStream));
            il.DeclareLocal(typeof(byte[]));

            il.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(new Type[0]) ?? throw new InvalidOperationException("Could not get MemoryStream constructor...")); // MemoryStream m = new MemoryStream()
            il.Emit(OpCodes.Stloc_0);                                                                                                                                      //
            #endregion

            for (int fi = 0; fi < fields.Count; fi++)
            {
                // PREP
                FieldInfo f = fields[fi].field;
                SerializationLibraryEntry entr = fields[fi].entry;

                var delType = typeof(SerializerDelegate<>).MakeGenericType(f.FieldType);
                paramArray[fi] = entr.SerializerPointer;
                #region Read Field

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, fi);
                il.Emit(OpCodes.Ldelem, typeof(Delegate));


                il.Emit(OpCodes.Ldarg_0); //  Puts the object thats being serialized on the eval stack
                il.Emit(OpCodes.Ldfld, f); // Push the field value onto the stack

                #endregion

                //STATE: Now the value of the field is on the stack.                          | STACKTOP | [FIELDVAL] | STACKEND |
                #region Convert To Bytes

                il.Emit(OpCodes.Callvirt, delType.GetMethod("Invoke") ?? throw new InvalidOperationException("Could not get invoke method on delegate type..."));
                il.Emit(OpCodes.Stloc_1);
                #endregion

                #region Optional Size Prefix
                if (!entr.IsFixedSize)
                {
                    il.Emit(OpCodes.Ldloc_0); // MemoryStream
                    il.Emit(OpCodes.Ldloc_1); // byte[], MemoryStream

                    il.Emit(OpCodes.Ldlen); // uint32, MemoryStream
                    il.Emit(OpCodes.Call, uintSer); // byte[], MemoryStream

                    il.Emit(OpCodes.Call, arrConv); // ReadOnlySpan<byte>, MemoryStream
                    il.Emit(OpCodes.Callvirt, msWrite); // ...
                }
                #endregion

                #region Write Bytes



                il.Emit(OpCodes.Ldloc_0);
                il.Emit(OpCodes.Ldloc_1);
                il.Emit(OpCodes.Call, arrConv); // Convert to span for memory stream write
                il.Emit(OpCodes.Callvirt, msWrite); // Write to stream

                #endregion

            }

            #region Return Bytes
            il.Emit(OpCodes.Ldloc_0);
            il.Emit(OpCodes.Callvirt, typeof(MemoryStream).GetMethod("ToArray") ?? throw new InvalidOperationException("Could not find MemoryStream.ToArray()"));
            il.Emit(OpCodes.Ret);
            #endregion

            #endregion

            var del = (Func<T, Delegate[], byte[]>)m.CreateDelegate(typeof(Func<T, Delegate[], byte[]>));
            return (ob) =>
            {
                byte[] res = del(ob, paramArray);
                if (size != null && res.Length != size) throw new InvalidOperationException("Created buffer was of size " + res.Length + " not the expected " + size);
                return res;
            };
        }

        private static DeserializerDelegate<T> buildDefaultDeserializer(List<FieldArgs> fields, SerializationLibrary l, int? size)
        {
            Delegate[] paramArray = new Delegate[fields.Count];

            var uintDes = typeof(DefaultSerializers).GetMethod("UInt32_Deserialize", new Type[1] { typeof(ReadOnlyMemory<byte>) }) ?? throw new InvalidOperationException("Could not find uint converter");

            DynamicMethod m = new DynamicMethod("EMITD_compdeser_" + typeof(T), typeof(T), new Type[3] { typeof(ReadOnlyMemory<byte>), typeof(Delegate[]), typeof(Type) }, true);
            var il = m.GetILGenerator();

            var bufferIndex = il.DeclareLocal(typeof(int));
            var obj = il.DeclareLocal(typeof(T));
            var len = il.DeclareLocal(typeof(uint));

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, bufferIndex);

            

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call, typeof(Activator).GetMethod("CreateInstance", new Type[1] { typeof(Type) }) ?? throw new("Could not get activator method"));
            il.Emit(OpCodes.Stloc, obj);

            for (int i = 0; i < fields.Count; i++)
            {
                FieldInfo field = fields[i].field;
                SerializationLibraryEntry entry = fields[i].entry;

                var delType = typeof(DeserializerDelegate<>).MakeGenericType(field.FieldType);
                paramArray[i] = entry.DeserializerPointer;

                if(typeof(T).IsValueType) il.Emit(OpCodes.Ldloca_S, obj);
                else il.Emit(OpCodes.Ldloc, obj);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem, typeof(Delegate));

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

                
                il.Emit(OpCodes.Callvirt, delType.GetMethod("Invoke") ?? throw new("No invoke!"));
                il.Emit(OpCodes.Stfld, field);
            }
            il.Emit(OpCodes.Ldloc, obj);
            il.Emit(OpCodes.Ret);

            

            var del = (Func<ReadOnlyMemory<byte>, Delegate[], Type, T>)m.CreateDelegate<Func<ReadOnlyMemory<byte>, Delegate[], Type, T>>();
            return (m) => {
                if (size != null && m.Length != size) throw new ArgumentException("Need buffer of len " + size + ", only got len " + m.Length);
                return del(m, paramArray, typeof(T)); 
            };
        }

        private static (int?, SerializerDelegate<T>, DeserializerDelegate<T>) getConstructorArgs(Type t, SerializationLibrary l)
        {
            if (t.IsArray || t.IsAbstract || t.IsEnum) throw new InvalidOperationException("Cannot be used on arrays/abstract/enum types");

            List<FieldArgs> args = new List<FieldArgs>();
            int? fixedSize = 0;

            foreach (var field in RuntimeReflectionExtensions.GetRuntimeFields(typeof(T)))
            {
                if (field.IsStatic || field.HasAttribute<DontSerializeAttribute>()) continue;
                if ((field.IsPrivate && (field.HasAttribute<ForceSerializationAttribute>() || AutoSerializePrivateFields)) || field.IsPublic) try
                {
                        SerializationLibraryEntry entr = l.GetEntry(field.FieldType);
                        args.Add(new()
                        {
                            field = field,
                            entry = entr,
                        });
                        if (fixedSize != null) fixedSize =  entr.IsFixedSize ? fixedSize + entr.FixedSize : null;
                        
                }
                catch (SerializationEntryNotFoundException)
                {
                    throw new MissingSerializationDependencyException(field.FieldType);
                }    
            }

            return (fixedSize, buildSerializer(args, l, fixedSize), buildDefaultDeserializer(args, l, fixedSize));

        }

        private CompositeLibraryEntry((int?, SerializerDelegate<T>, DeserializerDelegate<T>) args) : base(args.Item1, args.Item2, args.Item3) { }
        
        public CompositeLibraryEntry(SerializationLibrary l) : this(getConstructorArgs(typeof(T), l)) { }
    }

    internal static class ExtentionMethods
    {
        public static bool HasAttribute(this MemberInfo i, Type attrType)
        {
            return i.GetCustomAttribute(attrType) != null;
        }

        public static bool HasAttribute<T>(this MemberInfo i)
        {
            return i.HasAttribute(typeof(T));
        }
    }
}
