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
    }
}