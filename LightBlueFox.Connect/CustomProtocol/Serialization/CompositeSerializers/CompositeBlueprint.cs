using LightBlueFox.Connect.Util;
using System.Reflection;
using System.Reflection.Emit;

namespace LightBlueFox.Connect.CustomProtocol.Serialization.CompositeSerializers
{
    public class CompositeBlueprint<T> : SerializationLibraryEntry<T>
    {

        private static SerializerDelegate<T> buildSerializer(List<FieldInfo> fields, SerializationLibrary l)
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
                var f = fields[fi];
                SerializationLibraryEntry? entr = null;
                try
                {
                    entr = l.GetEntry(f.FieldType);
                }
                catch (SerializationNotFoundException ex)
                {
                    throw new MissingSerializationDependencyException(f.FieldType);
                }
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
                if (!entr.Attribute.IsFixedSize)
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
                return del(ob, paramArray);
            };
        }

        private static DeserializerDelegate<T> buildDefaultDeserializer(List<FieldInfo> fields, SerializationLibrary l)
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
                var field = fields[i];
                SerializationLibraryEntry? entry = null;
                try
                {
                    entry = l.GetEntry(field.FieldType);
                }
                catch (KeyNotFoundException ex)
                {
                    throw new MissingSerializationDependencyException(field.FieldType);
                }

                var delType = typeof(DeserializerDelegate<>).MakeGenericType(field.FieldType);
                paramArray[i] = entry.DeserializerPointer;

                il.Emit(OpCodes.Ldloca_S, obj);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldc_I4, i);
                il.Emit(OpCodes.Ldelem, typeof(Delegate));


                if (!entry.Attribute.IsFixedSize)
                {
                    il.DoSlice(bufferIndex, intLen: sizeof(uint));
                    il.Emit(OpCodes.Call, uintDes);
                    il.Emit(OpCodes.Stloc, len);
                    il.DoSlice(bufferIndex, len, null);
                }
                else
                {
                    il.DoSlice(bufferIndex, intLen: entry.Attribute.FixedSize ?? throw new("Attribute says fixed size but fixed size is null!"));
                }

                il.Emit(OpCodes.Callvirt, delType.GetMethod("Invoke") ?? throw new("No invoke!"));
                il.Emit(OpCodes.Stfld, field);
            }

            il.Emit(OpCodes.Ldloc, obj);
            il.Emit(OpCodes.Ret);

            var del = (Func<ReadOnlyMemory<byte>, Delegate[], Type, T>)m.CreateDelegate<Func<ReadOnlyMemory<byte>, Delegate[], Type, T>>();
            return (m) => { return del(m, paramArray, typeof(T)); };
        }

        private static (SerializerDelegate<T>, DeserializerDelegate<T>) buildSerializationMethods(Type t, SerializationLibrary l)
        {
            if (t.IsArray || t.IsAbstract || t.IsEnum) throw new InvalidOperationException("Cannot be used on arrays/abstract/enum types");

            List<FieldInfo> types = new List<FieldInfo>();

            foreach (var field in t.GetFields())
            {
                if (field.IsStatic || field.HasAttribute<DontSerializeAttribute>()) continue;
                if ((field.IsPrivate && field.HasAttribute<ForceSerializationAttribute>()) || field.IsPublic) types.Add(field);
            }

            return (buildSerializer(types, l), buildDefaultDeserializer(types, l));

        }

        public CompositeBlueprint(SerializationAttribute attr, SerializationLibrary l) : base(attr, buildSerializationMethods(typeof(T), l))
        {
        }
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
