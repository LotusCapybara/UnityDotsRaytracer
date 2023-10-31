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

        // computed values
        public float3 centerPoint;
        public float3 p0p1;
        public float3 p0p2;
        public float3 edgeA;
        public float3 edgeB;
        public float3 faceNormal;
        public BoundsBox bounds;
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetVertexPos(int index)
        {
            if (index == 0)
                return posA;
            if (index == 1)
                return posB;
            if (index == 2)
                return posC;

            throw new IndexOutOfRangeException();
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float3 GetVertexNormal(int index)
        {
            if (index == 0)
                return normalA;
            if (index == 1)
                return normalB;
            if (index == 2)
                return normalC;

            throw new IndexOutOfRangeException();
        }
        
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
        

        public void Compute()
        {
            centerPoint = (posA + posB + posC) / 3f;
            faceNormal = (normalA + normalB + normalC) / 3f;

            edgeA = posB - posA;
            edgeB = posC - posB;

            p0p1 = posB - posA;
            p0p2 = posC - posA;

            bounds = new BoundsBox();
            bounds.ExpandWithTriangle(this);
        }
    }
}