using Unity.Collections;

namespace CapyTracerCore.Core
{
    public struct RenderScene
    {
        public TracerSettings settings;
        public BoundsBox bounds;
        public NativeArray<RenderMaterial> materials;
        public NativeArray<RenderTriangle> triangles;
        public NativeArray<RenderLight> lights;
        public NativeArray<StackBVHNode> bvhNodes;
    }
}