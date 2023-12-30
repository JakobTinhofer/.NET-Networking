using System.Reflection;
using System.Reflection.Emit;

namespace LightBlueFox.Connect.Util
{
    /// <summary>
    /// A collection of helper and extension methods often used in the serialization code.
    /// </summary>
    internal static class SerializationHelpers
    {
        static readonly MethodInfo slice = typeof(ReadOnlyMemory<byte>).GetMethod("Slice", new[] { typeof(int), typeof(int) }) ?? throw new InvalidOperationException("Could not find slice on memory!");
        public static void DoSlice(this ILGenerator il, LocalBuilder from, LocalBuilder? len = null, int? intLen = null, bool updateIndex = true, int argIndex = 0, bool debug = false)
        {
            if (len == null == (intLen == null)) throw new ArgumentException("Can either provide int len or local var!");
            if (debug != true) il.Emit(OpCodes.Ldarga_S, 0);
            il.Emit(OpCodes.Ldloc, from);
            if (intLen != null) il.Emit(OpCodes.Ldc_I4, intLen ?? 0);
            else if (len != null) il.Emit(OpCodes.Ldloc, len);

            if (debug)
            {
                il.WriteLineInt();
                il.WriteLineInt();
                il.DoSlice(from, len, intLen, updateIndex, argIndex, false);
                return;
            }

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

        public static void WriteLineInt(this ILGenerator il)
        {
            il.Emit(OpCodes.Call, typeof(Console).GetMethod("WriteLine", new[] { typeof(int) }) ?? throw new Exception());
        }

        public static void ForEachMember(Type[] types, Action<MethodInfo, Type>? methodAction = null, Action<Type>? typeAction = null, Action<FieldInfo, Type>? fieldAction = null)
        {
            foreach (var t in types)
            {
                List<Type> nestedTypes = new() { t };
                List<Type> nextNestedTypes = new();
                while (nestedTypes.Count > 0)
                {
                    foreach (var nt in nestedTypes)
                    {
                        if (typeAction != null) typeAction(nt);


                        if (methodAction != null)
                            foreach (var m in nt.GetMethods())
                            {
                                methodAction(m, t);
                            }

                        if (fieldAction != null)
                            foreach (var m in nt.GetFields())
                            {
                                fieldAction(m, t);
                            }

                        nextNestedTypes.AddRange(nt.GetNestedTypes());
                    }
                    nestedTypes = nextNestedTypes;
                    nextNestedTypes = new();
                }
            }
        }



    }
}
