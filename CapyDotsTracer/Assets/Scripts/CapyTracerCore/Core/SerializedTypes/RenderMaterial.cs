using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderMaterial
    {
        public float4 color;
        public float roughness;
        public bool isEmissive;
    }
}