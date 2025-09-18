using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Mathematics;

namespace Unity.Collections.LowLevel.Unsafe
{
    /// <summary>
    /// Unmanaged version of <see cref="byte"/> <see cref="Span{T}"/>.
    /// Not required to hold a valid sequence of utf-8 bytes.
    /// </summary>
    public readonly unsafe struct ByteSpan :
        INativeList<byte>,
        IUTF8Bytes,
        IComparable<ByteSpan>,
        IEquatable<ByteSpan>
    {
        public readonly byte* Ptr;
        public readonly int LengthField;

        public static ByteSpan Empty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(null, 0);
        }

        public int Length
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get => CollectionHelper.AssumePositive(LengthField);
            set => throw new NotSupportedException();
        }

        public readonly int Capacity
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length;
            set => throw new NotSupportedException();
        }

        public readonly bool IsEmpty
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Length == 0;
        }

        public byte this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            readonly get
            {
                CollectionHelper.CheckIndexInRange(index, LengthField);
                return Ptr[index];
            }

            set => throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ByteSpan(byte* ptr, int length)
        {
            Ptr = ptr;
            LengthField = length;

            CheckPtr(ptr, length);
            CheckLengthInRange(length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly byte* GetUnsafePtr()
        {
            return Ptr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool TryResize(int newLength, NativeArrayOptions clearOptions = NativeArrayOptions.ClearMemory)
        {
            Length = newLength;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ref byte ElementAt(int index)
        {
            CollectionHelper.CheckIndexInRange(index, LengthField);
            return ref Ptr[index];
        }

        public void Clear()
        {
            throw new NotSupportedException();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int CompareTo(ByteSpan other)
        {
            if (Constant.IsConstantExpression(true))
            {
                // Burst

                if (Constant.IsConstantExpression(Equals(other)) && Equals(other))
                {
                    return 0;
                }

                int count = math.min(Length, other.Length);
                int cmp = UnsafeUtility.MemCmp(Ptr, other.Ptr, count);

                return cmp == 0 ? Length - other.Length : cmp;
            }
            else
            {
                // No Burst

                int result = 0;
                CompareToNoBurst(other, ref result);
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(ByteSpan other)
        {
            if (Constant.IsConstantExpression(true))
            {
                // Burst

                if (Constant.IsConstantExpression(IsEmpty) &&
                    Constant.IsConstantExpression(other.IsEmpty))
                {
                    if (IsEmpty != other.IsEmpty)
                    {
                        return false;
                    }
                    else if (IsEmpty && other.IsEmpty)
                    {
                        return true;
                    }
                }

                if (Constant.IsConstantExpression(Ptr == other.Ptr))
                {
                    if (Ptr == other.Ptr)
                    {
                        return Length == other.Length;
                    }
                }

                if (Length != other.Length)
                {
                    // Different lengths
                    return false;
                }

                return UnsafeUtility.MemCmp(Ptr, other.Ptr, Length) == 0;
            }
            else
            {
                // No Burst

                bool result = false;
                EqualsNoBurst(other, ref result);
                return result;
            }
        }

        public override readonly bool Equals(object obj)
        {
            return obj is ByteSpan other && Equals(other);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override readonly int GetHashCode()
        {
            return xxHash3.Hash64(Ptr, Length).GetHashCode();
        }

        public override readonly string ToString()
        {
            return new string((sbyte*)Ptr, startIndex: 0, length: LengthField);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ByteSpan Slice(int startIndex)
        {
            int resultLength = Length - startIndex;
            FixedStringMethods.CheckSubstringInRange(LengthField, startIndex, resultLength);

            return new ByteSpan(Ptr + startIndex, resultLength);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly ByteSpan Slice(int startIndex, int length)
        {
            int resultLength = math.min(length, Length - startIndex);
            FixedStringMethods.CheckSubstringInRange(LengthField, startIndex, resultLength);

            return new ByteSpan(Ptr + startIndex, resultLength);
        }

        [BurstDiscard]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CompareToNoBurst(ByteSpan other, ref int result)
        {
            result = ((ReadOnlySpan<byte>)this).SequenceCompareTo(other);
        }

        [BurstDiscard]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void EqualsNoBurst(ByteSpan other, ref bool result)
        {
            result = ((ReadOnlySpan<byte>)this).SequenceEqual(other);
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckPtr(byte* ptr, int length)
        {
            if (ptr == null && (uint)length > 0)
            {
                throw new ArgumentException($"Ptr cannot be null with non-zero length.");
            }
        }

        [Conditional("ENABLE_UNITY_COLLECTIONS_CHECKS")]
        [Conditional("UNITY_DOTS_DEBUG")]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private readonly void CheckLengthInRange(int length)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException($"Length {length} must be positive.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Span<byte>(ByteSpan self)
        {
            return new Span<byte>(self.Ptr, self.LengthField);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ReadOnlySpan<byte>(ByteSpan self)
        {
            return new ReadOnlySpan<byte>(self.Ptr, self.LengthField);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteSpan(UnsafeText unsafeText)
        {
            return new ByteSpan(unsafeText.GetUnsafePtr(), unsafeText.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator ByteSpan(NativeText nativeText)
        {
            return new ByteSpan(nativeText.GetUnsafePtr(), nativeText.Length);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(ByteSpan lhs, ByteSpan rhs)
        {
            return lhs.Equals(rhs);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(ByteSpan lhs, ByteSpan rhs)
        {
            return !lhs.Equals(rhs);
        }
    }
}
