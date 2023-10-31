using System.Runtime.CompilerServices;
using CapyTracerCore.Core;
using CapyTracerCore.Core.Functions;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct Job_IndirectSamples : IJobParallelFor
{
    [ReadOnly] 
    public NativeArray<float3> randomNumbers;
    
    [ReadOnly]
    public int iteration;

    [ReadOnly]
    public int maxBounces;
    
    [ReadOnly]
    public RenderScene scene;

    [ReadOnly]
    public NativeArray<TriangleHitInfo> cameraHits;

    public NativeArray<float4> indirectColors;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private float3 GetNextRandomV3(ref int displacement)
    {
        displacement ++;
        if (displacement >= randomNumbers.Length)
            displacement = 0;

        return randomNumbers[displacement];
    }

    public void Execute(int index)
    {
        TriangleHitInfo surfaceHit = cameraHits[index];
        
        if(!surfaceHit.hitFound)
            return;
        
        int rndDisplacement = index + iteration;
        GetNextRandomV3(ref rndDisplacement);
        
        RenderRay bounceRay = new RenderRay();
            
        float4 indirectColor = new float4(0, 0, 0, 1);
        
        for (int b = 0; b < maxBounces; b++)
        {
            bool foundGoodBRDF = false;
            float brdf = 0;
        
            while (!foundGoodBRDF)
            {
                bounceRay.direction = GetNextRandomV3(ref rndDisplacement);
                if (math.dot(bounceRay.direction, surfaceHit.normal) < 0)
                    bounceRay.direction *= -1f; 
        
                bounceRay.origin = surfaceHit.position + surfaceHit.normal * 0.001f;
        
                float roughness = scene.materials[surfaceHit.materialIndex].roughness;
            
                float diffuseTerm = 0; 
                float specularTerm = 0;
        
                if (roughness > 0f)
                {
                    diffuseTerm = math.dot(bounceRay.direction, surfaceHit.normal);
                }
            
                if (roughness < 1f &&  GetNextRandomV3(ref rndDisplacement).x > roughness)
                {
                    float3 reflectionVector = math.reflect(surfaceHit.incomingDirection, surfaceHit.normal);
                    bounceRay.direction = reflectionVector;
        
                    float3 H = bounceRay.direction + surfaceHit.incomingDirection;
                    specularTerm = LightFunctions.GetBlinnPhong(H, surfaceHit.normal, roughness);
                }
            
            
                brdf = math.saturate(diffuseTerm + specularTerm);
        
                foundGoodBRDF = true;
                if (brdf < 0.05f &&  GetNextRandomV3(ref rndDisplacement).x < 0.5f)
                {
                    foundGoodBRDF = false;
                }
            }
        
        
            TriangleHitInfo bounceHit = TraceCast.TryHitTriangle(bounceRay, scene, math.INFINITY, false);
            
            if (bounceHit.hitFound)
            {
                float4 colorsOnSurface = TraceFunctions.GetLitColorOnSurface(bounceHit, scene);
                
                indirectColor += colorsOnSurface * brdf * (1f / 3.14f);
                surfaceHit = bounceHit;
                
            }
            else
            {
                break;
            }
        }
        
        float iterationWeight = 1f / (iteration + 1f);
        indirectColors[index] = indirectColors[index] * (1f - iterationWeight) +  indirectColor * iterationWeight ;
    }
}
