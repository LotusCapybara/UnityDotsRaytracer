using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public struct BoundsBox
    {
        public float3 min;
        public float3 max;
        public float3 center;

        public BoundsBox(float3 boundMin, float3 boundMax)
        {
            min = boundMin;
            max = boundMax;
            center = new float3(
                min.x + (max.x - min.x) * 0.5f,
                min.y + (max.y - min.y) * 0.5f,
                min.z + (max.z - min.z) * 0.5f
            );
        }

        public float3 GetSize()
        {
            return new float3(max.x - min.x, max.y - min.y, max.z - min.z);
        }

        public void ExpandWithBounds(in BoundsBox other)
        {
            ExpandWithPoint(other.min);
            ExpandWithPoint(other.max);
        }

        public void ExpandWithTriangle(in RenderTriangle triangle)
        {
            ExpandWithPoint(triangle.posA);
            ExpandWithPoint(triangle.posB);
            ExpandWithPoint(triangle.posC);
        }

        public void ExpandWithPoint(float3 p)
        {
            if (p.x < min.x)
                min.x = p.x;
            if (p.y < min.y)
                min.y = p.y;
            if (p.z < min.z)
                min.z = p.z;

            if (p.x > max.x)
                max.x = p.x;
            if (p.y > max.y)
                max.y = p.y;
            if (p.z > max.z)
                max.z = p.z;

            center = GetCenter();
        }


        public float3 GetCenter()
        {
            return new float3(
                min.x + (max.x - min.x) * 0.5f,
                min.y + (max.y - min.y) * 0.5f,
                min.z + (max.z - min.z) * 0.5f
            );
        }

        public bool IsPointInside(float3 point)
        {
            if (point.x < min.x || point.x > max.x)
                return false;

            if (point.y < min.y || point.y > max.y)
                return false;

            if (point.z < min.z || point.z > max.z)
                return false;

            return true;
        }

        public (BoundsBox, BoundsBox) GetSplit(int axis, float splitRatio)
        {
            center = GetCenter();
            BoundsBox boundsA = this;
            BoundsBox boundsB = this;

            float mid = min[axis] + (max[axis] - min[axis]) * splitRatio;

            boundsA.min[axis] = min[axis];
            boundsA.max[axis] = mid;
            boundsB.min[axis] = mid;
            boundsB.max[axis] = max[axis];

            return (boundsA, boundsB);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float SquaredDistToPoint( in float3 p)
        {
            float Check(float pn, float  bmin, float bmax )
            {
                float result = 0;
                float  v = pn;
 
                if ( v < bmin ) 
                {             
                    float val = (bmin - v);             
                    result += val * val;         
                }         
         
                if ( v > bmax )
                {
                    float val = (v - bmax);
                    result += val * val;
                }

                return result;
            };
 
            // Squared distance
            float sq = 0f;
 
            sq += Check( p.x, min.x, max.x );
            sq += Check( p.y, min.y, max.y );
            sq += Check( p.z, min.z, max.z );
 
            return sq;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public (float3, float3, float3, float3, float3, float3, float3, float3) GetCorners()
        {
            float3 size = GetSize();


            return (
                min, min + new float3(size.x, 0, 0), min + new float3(0, 0, size.z), min + new float3(size.x, 0, size.z),
                max, max - new float3(size.x, 0, 0), max - new float3(0, 0, size.z), max - new float3(size.x, 0, size.z));
        }
    }
}