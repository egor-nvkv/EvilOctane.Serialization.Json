using System.Runtime.CompilerServices;
using Unity.Burst.CompilerServices;
using SystemUnsafe = System.Runtime.CompilerServices.Unsafe;

namespace Unity.Collections.LowLevel.Unsafe
{
    public static unsafe class UnsafeUtility2
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void TinyMemCpy(void* destination, void* source, long size)
        {
            Memory.CheckByteCountIsReasonable(size);

            if (Constant.IsConstantExpression(true))
            {
                // Burst

                if (Constant.IsConstantExpression(size))
                {
                    // Immediate size

                    UnsafeUtility.MemCpy(destination, source, size);
                }
                else if (Constant.IsConstantExpression(IsTinyNonZero(size)) && IsTinyNonZero(size))
                {
                    // Tiny size - inline

                    byte* dest = (byte*)destination;
                    byte* src = (byte*)source;

                    switch (size)
                    {
                        default:
                        case 1:
                            dest[0] = src[0];
                            break;

                        case 2:
                            dest[0] = src[0];
                            dest[1] = src[1];
                            break;

                        case 3:
                            dest[0] = src[0];
                            dest[1] = src[1];
                            dest[2] = src[2];
                            break;

                        case 4:
                            dest[0] = src[0];
                            dest[1] = src[1];
                            dest[2] = src[2];
                            dest[3] = src[3];
                            break;
                    }
                }
                else
                {
                    // Default

                    UnsafeUtility.MemCpy(destination, source, size);
                }
            }
            else
            {
                // No Burst

                SystemUnsafe.CopyBlock(destination, source, (uint)size);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static bool IsTinyNonZero(long size)
        {
            return size is >= 1 and <= 4;
        }
    }
}
