using Unity.Mathematics;

namespace CapyTracerCore.Core.Functions
{
    public static class TraceFunctions
    {
        public static float4 GetLitColorOnSurface(in TriangleHitInfo surfaceHit, in RenderScene scene)
        {
            float4 color = new float4(0, 0, 0, 1);

            if (scene.materials[surfaceHit.materialIndex].isEmissive)
            {
                color += scene.materials[surfaceHit.materialIndex].color;
            }
            else
            {
                foreach (var light in scene.lights)
                { 
                    color += light.GetColorContribution(surfaceHit, scene);
                }
            
                color *= scene.materials[surfaceHit.materialIndex].color;
            }

            return color;
        }
    }
}