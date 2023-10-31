using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public struct TriangleHitInfo
    {
        public bool hitFound;
        public float distance;
        public float3 normal;
        public float3 position;
        public float3 incomingDirection;
        public int materialIndex;
    }
}