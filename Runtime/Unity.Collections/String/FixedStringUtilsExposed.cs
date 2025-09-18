using System.Runtime.CompilerServices;

namespace Unity.Collections
{
    public static class FixedStringUtilsExposed
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Base2ToBase10(ref ulong mantissa10, ref int exponent10, float input)
        {
            FixedStringUtils.Base2ToBase10(ref mantissa10, ref exponent10, input);
        }
    }
}
