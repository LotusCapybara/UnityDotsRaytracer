using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SerializedScene
    {
        public float3 boundMin;
        public float3 boundMax;
        public SerializedCamera camera;
        public int qtyMaterials;
        public RenderMaterial[] materials;
        public int qtyTriangles;
        public RenderTriangle[] triangles;
        public int qtyLights;
        public RenderLight[] lights;
    }
}