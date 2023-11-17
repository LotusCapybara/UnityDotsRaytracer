using System.IO;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class BinaryWriteExtensions
    {
        public static void WriteBinary(this float3 f3, BinaryWriter writer)
        {
            writer.Write(f3.x);
            writer.Write(f3.y);
            writer.Write(f3.z);
        }
        
        public static void WriteBinary(this float4 f4, BinaryWriter writer)
        {
            writer.Write(f4.x);
            writer.Write(f4.y);
            writer.Write(f4.z);
            writer.Write(f4.w);
        }
    
        public static void WriteBinary(this RenderMaterial mat, BinaryWriter writer)
        {
            mat.color.WriteBinary(writer);
            writer.Write(mat.roughness);
            writer.Write(mat.isEmissive);
        }
        
        public static void WriteBinary(this SerializedCamera cam, BinaryWriter writer)
        {
            cam.position.WriteBinary(writer);
            cam.forward.WriteBinary(writer);
            cam.right.WriteBinary(writer);
            cam.up.WriteBinary(writer);
            writer.Write(cam.horizontalSize);
            writer.Write(cam.fov);
        }
        
        
        public static void WriteBinary(this RenderTriangle t, BinaryWriter writer)
        {
            t.posA.WriteBinary(writer);
            t.posB.WriteBinary(writer);
            t.posC.WriteBinary(writer);
            t.normalA.WriteBinary(writer);
            t.normalB.WriteBinary(writer);
            t.normalC.WriteBinary(writer);
            writer.Write(t.materialIndex);
        }
        
        public static void WriteBinary(this RenderLight light, BinaryWriter writer)
        {
            light.color.WriteBinary(writer);
            light.position.WriteBinary(writer);
            light.forward.WriteBinary(writer);
            writer.Write(light.range);
            writer.Write(light.intensity);
            writer.Write(light.angle);
            writer.Write(light.type);
        }
        
    }
}