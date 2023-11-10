using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace LightBlueFox.Connect.Util
{
    internal static class SerializationHelpers
    {
        static MethodInfo slice = typeof(ReadOnlyMemory<byte>).GetMethod("Slice", new[] { typeof(int), typeof(int) }) ?? throw new InvalidOperationException("Could not find slice on memory!");
        public static void DoSlice(this ILGenerator il, LocalBuilder from, LocalBuilder? len = null, int? intLen = null, bool updateIndex = true, int argIndex = 0)
        {
            if ((len == null) == (intLen == null)) throw new ArgumentException("Can either provide int len or local var!");
            il.Emit(OpCodes.Ldarga_S, 0);
            il.Emit(OpCodes.Ldloc, from);
            if (intLen != null) il.Emit(OpCodes.Ldc_I4, intLen ?? 0);
            else if (len != null) il.Emit(OpCodes.Ldloc, len);

            il.Emit(OpCodes.Call, slice);

            if (updateIndex)
            {
                il.Emit(OpCodes.Ldloc, from);
                if (intLen != null) il.Emit(OpCodes.Ldc_I4, intLen ?? 0);
                else if (len != null) il.Emit(OpCodes.Ldloc, len);
                il.Emit(OpCodes.Add);
                il.Emit(OpCodes.Stloc, from);
            }
        }
    }
}
