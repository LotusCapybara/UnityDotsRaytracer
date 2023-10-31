using Unity.Collections;

namespace CapyTracerCore.Core
{
    public struct RenderScene
    {
        public NativeArray<RenderMaterial> materials;
        public NativeArray<RenderTriangle> triangles;
        public NativeArray<RenderLight> lights;
    }
}