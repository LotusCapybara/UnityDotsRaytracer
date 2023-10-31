using System.Runtime.InteropServices;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RenderMesh
    {
        public RenderTriangle[] triangles;
    }
}