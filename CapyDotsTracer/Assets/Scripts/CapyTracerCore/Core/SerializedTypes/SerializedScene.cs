using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct SerializedScene
    {
        public float3 boundMin;
        public float3 boundMax;
        public RenderMaterial[] materials;
        public SerializedCamera camera;
        public RenderMesh[] meshes;
        public RenderLight[] lights;
    }
}