using System.IO;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class SceneBinaryRead
    {
        public static float3 ReadFloat3(BinaryReader reader)
        {
            float3 f3 = float3.zero;
            f3.x =  reader.ReadSingle();
            f3.y =  reader.ReadSingle();
            f3.z =  reader.ReadSingle();
            return f3;
        }
        
        public static float4 ReadFloat4(BinaryReader reader)
        {
            float4 f4 = float4.zero;
            f4.x =  reader.ReadSingle();
            f4.y =  reader.ReadSingle();
            f4.z =  reader.ReadSingle();
            f4.w =  reader.ReadSingle();
            return f4;
        }
    
        public static RenderMaterial ReadMaterial(BinaryReader reader)
        {
            RenderMaterial mat = new RenderMaterial();

            mat.color = ReadFloat4(reader);
            mat.roughness = reader.ReadSingle();
            mat.isEmissive = reader.ReadBoolean();

            return mat;
        }
        
        public static SerializedCamera ReadCamera(BinaryReader reader)
        {
            SerializedCamera cam = new SerializedCamera();

            cam.position = ReadFloat3(reader);
            cam.forward = ReadFloat3(reader);
            cam.right = ReadFloat3(reader);
            cam.up = ReadFloat3(reader);
            cam.horizontalSize = reader.ReadSingle();
            cam.fov = reader.ReadSingle();
            

            return cam;
        }
        
        
        public static RenderTriangle ReadTriangle(BinaryReader reader)
        {
            RenderTriangle t = new RenderTriangle();

            t.posA = ReadFloat3(reader);
            t.posB = ReadFloat3(reader);
            t.posC = ReadFloat3(reader);
            
            t.normalA = ReadFloat3(reader);
            t.normalB = ReadFloat3(reader);
            t.normalC = ReadFloat3(reader);
            
            t.materialIndex = reader.ReadInt32();

            return t;
        }
        
        public static RenderLight ReadLight(BinaryReader reader)
        {
            RenderLight light = new RenderLight();

            light.color = ReadFloat4(reader);
            light.position = ReadFloat3(reader);
            light.forward = ReadFloat3(reader);
            
            light.range = reader.ReadSingle();
            light.intensity = reader.ReadSingle();
            light.angle = reader.ReadSingle();
            
            light.type = reader.ReadInt32();

            return light;
        }
        
    }
}