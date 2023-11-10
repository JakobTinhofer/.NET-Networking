using LightBlueFox.Connect.Util;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.CustomProtocol.Serialization
{
    internal class ArraySerializationLibraryEntry<T> : SerializationLibraryEntry<T[]>
    {
        public static SerializerDelegate<T[]> GenerateArraySerializer(SerializationLibraryEntry baseEntry)
        {
            var uintSer = typeof(DefaultSerializers).GetMethod("UInt32_Serialize", new Type[1] { typeof(uint) }) ?? throw new InvalidOperationException("Could not find uint converter...");
            var arrConv = typeof(ReadOnlySpan<byte>).GetMethod("op_Implicit", new Type[1] { typeof(byte[]) }) ?? throw new Exception("Failed to get operator");
            var msWrite = typeof(MemoryStream).GetMethod("Write", new Type[1] { typeof(ReadOnlySpan<byte>) }) ?? throw new Exception("Failed to get Write");
            var delType = typeof(SerializerDelegate<T>);
            var delInvoke = delType.GetMethod("Invoke") ?? throw new Exception("Failed to get del invoke");

            DynamicMethod m = new DynamicMethod("EMITD_arrayser_" + typeof(T), typeof(byte[]), new Type[2] { typeof(T[]), typeof(Delegate) }, true);
            var il = m.GetILGenerator();
            var stream = il.DeclareLocal(typeof(MemoryStream));
            var buffer = baseEntry.Attribute.IsFixedSize ? null : il.DeclareLocal(typeof(byte[]));
            var len = il.DeclareLocal(typeof(int));
            var i = il.DeclareLocal(typeof(int));
            var loopCondition = il.DefineLabel();
            var loopIterator = il.DefineLabel();
            var loopBody = il.DefineLabel();


            il.Emit(OpCodes.Newobj, typeof(MemoryStream).GetConstructor(new Type[0]) ?? throw new InvalidOperationException("Could not get MemoryStream constructor...")); // MemoryStream m = new MemoryStream()
            il.Emit(OpCodes.Stloc, stream);

            #region Length Prefix

            // Get the length of the array
            il.Emit(OpCodes.Ldloc, stream);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Stloc, len);

            // Write the length of the array to the buffer
            il.Emit(OpCodes.Ldloc, len);
            il.Emit(OpCodes.Call, uintSer);
            il.Emit(OpCodes.Call, arrConv);
            il.Emit(OpCodes.Callvirt, msWrite);

            // Loop setup. Set i to 0
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, i);

            il.Emit(OpCodes.Br_S, loopCondition);

            #endregion

            #region Loop Body
            il.MarkLabel(loopBody);
            il.Emit(OpCodes.Ldloc, stream);

            // Load delegate
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Castclass, delType);

            // Load the elem at index i of the first arg (the array to serialize
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldelem, typeof(T));

            // Serialize obj
            il.Emit(OpCodes.Callvirt, delInvoke);

            // For non fixed size
            if (buffer != null)
            {
                // Store in local then get buffer len
                il.Emit(OpCodes.Stloc, buffer);
                il.Emit(OpCodes.Ldloc, buffer);
                il.Emit(OpCodes.Ldlen);

                // Write buffer len
                il.Emit(OpCodes.Call, uintSer);
                il.Emit(OpCodes.Call, arrConv);
                il.Emit(OpCodes.Callvirt, msWrite);

                // Set up buffer write
                il.Emit(OpCodes.Ldloc, stream);
                il.Emit(OpCodes.Ldloc, buffer);
            }

            // Write buffer to stream
            il.Emit(OpCodes.Call, arrConv);
            il.Emit(OpCodes.Callvirt, msWrite);

            #endregion

            #region Iterator
            // Increase i by 1
            il.MarkLabel(loopIterator);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldc_I4, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, i);
            #endregion

            #region Loop condition
            // if i < len, jump to body
            il.MarkLabel(loopCondition);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldloc, len);
            il.Emit(OpCodes.Blt_S, loopBody);
            #endregion

            il.Emit(OpCodes.Ldloc, stream);
            il.Emit(OpCodes.Callvirt, typeof(MemoryStream).GetMethod("ToArray") ?? throw new("Could not get toArray"));
            il.Emit(OpCodes.Ret);

            var del = (Func<T[], Delegate, byte[]>)m.CreateDelegate(typeof(Func<T[], Delegate, byte[]>));
            return (ob) =>
            {
                return del(ob, baseEntry.SerializerPointer);
            };
        }

        public static DeserializerDelegate<T[]> GenerateArrayDeserializer(SerializationLibraryEntry baseEntry)
        {
            ReadOnlyMemory<byte> test;
            var uintDes = typeof(DefaultSerializers).GetMethod("UInt32_Deserialize", new Type[1] { typeof(ReadOnlyMemory<byte>)}) ?? throw new InvalidOperationException("Could not find uint converter");
            var span = typeof(ReadOnlyMemory<byte>).GetProperty("Span")?.GetGetMethod() ?? throw new InvalidOperationException("Could not find span on memory!");
            var delType = typeof(DeserializerDelegate<T>);
            DynamicMethod m = new DynamicMethod("EMITD_arraydeser_" + typeof(T), typeof(T[]), new Type[2] { typeof(ReadOnlyMemory<byte>), typeof(Delegate) }, true);
            var il = m.GetILGenerator();

            var loopBody = il.DefineLabel();
            var loopIterator = il.DefineLabel();
            var loopCondition = il.DefineLabel();

            int? isSizeConst = baseEntry.Attribute.FixedSize;

            var bufferIndex = il.DeclareLocal(typeof(int));
            var len = il.DeclareLocal(typeof(int));
            var readLen = il.DeclareLocal(typeof(int));
            var i = il.DeclareLocal(typeof(int));
            var resArray = il.DeclareLocal(typeof(T[]));

            il.Emit(OpCodes.Ldc_I4, 0);
            il.Emit(OpCodes.Stloc, bufferIndex);
            il.DoSlice(bufferIndex, intLen: 4);

            il.Emit(OpCodes.Call, uintDes);
            il.Emit(OpCodes.Stloc, len);

            il.Emit(OpCodes.Ldloc, len);
            il.Emit(OpCodes.Newarr, typeof(T));
            il.Emit(OpCodes.Stloc, resArray);

            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, i);
            il.Emit(OpCodes.Br_S, loopCondition);

            #region LoopBody
            il.MarkLabel(loopBody);
            if (isSizeConst != null) // Len of obj is already known
            {
                il.Emit(OpCodes.Ldc_I4, isSizeConst ?? 0);
                il.Emit(OpCodes.Stloc, readLen);
            }
            else // Read len of object, increasing the buffer index by 4
            {
                il.DoSlice(bufferIndex, intLen: 4);

                il.Emit(OpCodes.Call, uintDes);
                il.Emit(OpCodes.Stloc, readLen);
            }

            il.Emit(OpCodes.Ldloc, resArray);
            il.Emit(OpCodes.Ldloc, i);

            il.Emit(OpCodes.Ldarg_1);
            il.DoSlice(bufferIndex, readLen);
            il.Emit(OpCodes.Callvirt, delType.GetMethod("Invoke") ?? throw new("No Invoke!"));
            
            il.Emit(OpCodes.Stelem, typeof(T));

            #endregion

            #region LoopIterator
            il.MarkLabel(loopIterator);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldc_I4, 1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, i);
            #endregion

            #region LoopCondition
            il.MarkLabel(loopCondition);
            il.Emit(OpCodes.Ldloc, i);
            il.Emit(OpCodes.Ldloc, len);
            il.Emit(OpCodes.Blt_S, loopBody);
            #endregion

            il.Emit(OpCodes.Ldloc, resArray);
            il.Emit(OpCodes.Ret);

            Func<ReadOnlyMemory<byte>, Delegate, T[]> f = m.CreateDelegate<Func<ReadOnlyMemory<byte>, Delegate, T[]>>();
            return (mem) => f(mem, baseEntry.DeserializerPointer);
        }
        
        

        public ArraySerializationLibraryEntry(SerializationLibraryEntry baseTypeEntry) : base(new CustomSerializationAttribute<T[]>(), GenerateArraySerializer(baseTypeEntry), GenerateArrayDeserializer(baseTypeEntry))
        {
        }
    }
}
