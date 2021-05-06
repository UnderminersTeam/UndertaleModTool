using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UndertaleModLib.Util
{
    // BitArray class custom-made for the decompiler
    // Designed with performance in mind
    public class CustomBitArray
    {
        public int[] Array;
        public int Length;

        public CustomBitArray(int length)
        {
            Length = length;
            int arrayLength = ((length - 1) / 32) + 1;
            Array = new int[arrayLength];
        }

        public void SetAllTrue()
        {
            unsafe
            {
                fixed (int* ptr = &Array[0])
                {
                    uint* currPtr = (uint*)ptr;
                    int len = Array.Length;
                    for (int i = 0; i < len; i++)
                        *(currPtr++) = 0xFFFFFFFF;
                }
            }
        }

        public bool Get(int ind)
        {
            return (Array[ind / 32] & (1 << (ind % 32))) != 0;
        }

        public void SetTrue(int ind)
        {
            Array[ind / 32] |= (1 << (ind % 32));
        }

        public unsafe bool And(CustomBitArray other, int setIndex)
        {
            bool changed = false;

            int setIndexArr = setIndex / 32;

            fixed (int* ptr = &Array[0])
            {
                fixed (int* ptr2 = &other.Array[0])
                {
                    uint* currPtr = (uint*)ptr;
                    uint* otherPtr = (uint*)ptr2;

                    int i;
                    int len = Array.Length;
                    for (i = 0; i < len; i++)
                    {
                        uint before = *currPtr;
                        uint after = before;
                        after &= *otherPtr;
                        if (setIndexArr == i)
                            after |= ((uint)1 << (setIndex % 32));
                        if (before != after)
                        {
                            *currPtr = after;
                            currPtr++;
                            otherPtr++;
                            changed = true;
                            break;
                        }
                        currPtr++;
                        otherPtr++;
                    }
                    for (i++; i < len; i++)
                    {
                        *currPtr &= *otherPtr;
                        if (setIndexArr == i)
                            *currPtr |= ((uint)1 << (setIndex % 32));
                        currPtr++;
                        otherPtr++;
                    }
                }
            }

            return changed;
        }
    }
}
