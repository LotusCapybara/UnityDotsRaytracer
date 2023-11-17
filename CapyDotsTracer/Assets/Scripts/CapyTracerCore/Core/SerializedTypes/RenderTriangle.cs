using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderTriangle
    {
        public float3 posA;
        public float3 posB;
        public float3 posC;
        
        public float3 normalA;
        public float3 normalB;
        public float3 normalC;

        public int materialIndex;
        public int nodeIndex;

        // computed values
        // although you might think it's faster to cache these values, by not having them in memory you make
        // locality of data easier when iterating over all the triangles because the size of the struct is smaller
        // I ran some tests and having it like this instead of cached data it's between 4 and 8 times faster 
        // (in my tests at least). That's the power of the CPU cache!
        public float3 centerPoint => (posA + posB + posC) * 0.3333f;
        public float3 p0p1 => posB - posA;
        public float3 p0p2 => posC - posA;
        public float3 faceNormal => (normalA + normalB + normalC) * 0.3333f;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexPos(int index, float3 value)
        {
            if (index == 0)
            {
                posA = value;
                return;
            }

            if (index == 1)
            {
                posB = value;
                return;
            }

            if (index == 2)
            {
                posC = value;
                return;
            }
                

            throw new IndexOutOfRangeException();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetVertexNormal(int index, float3 value)
        {
            if (index == 0)
            {
                normalA = value;
                return;
            }

            if (index == 1)
            {
                normalB = value;
                return;
            }

            if (index == 2)
            {
                normalC = value;
                return;
            }

            throw new IndexOutOfRangeException();
        }
    }
}