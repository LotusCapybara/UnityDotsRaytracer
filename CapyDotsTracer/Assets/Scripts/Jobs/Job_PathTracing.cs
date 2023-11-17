using CapyTracerCore.Core;
using CapyTracerCore.Core.Functions;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

[BurstCompile(FloatPrecision.Medium, FloatMode.Fast)]
public struct Job_PathTracing : IJobParallelFor
{
    [NativeSetThreadIndex] 
    int threadId;

    public NativeArray<Random> randoms;

    [ReadOnly]
    public int iterations;
    
    [ReadOnly]
    public RenderScene scene;

    [ReadOnly]
    public NativeArray<RenderRay> cameraRays;

    public NativeArray<float4> indirectColors;

    public void Execute(int index)
    {
        Random rand = randoms[index];

        BRDFData brdfData = new BRDFData
        {
            diffuseMode = (byte) scene.settings.diffuseMode,
            specularMode = (byte) scene.settings.specularMode
        };

        RenderRay pathRay = cameraRays[index];
        float4 totalColor = PathRay(ref brdfData, ref pathRay, ref rand, 0);
        
        float iterationWeight = 1f / (iterations + 1f);
        indirectColors[index] = indirectColors[index] * (1f - iterationWeight) + totalColor * iterationWeight; 
        
        randoms[index] = rand;
    }

    private float4 PathRay(ref BRDFData brdfData, ref RenderRay pathRay, ref Random rand, int depth)
    {
        float4 rayEnergy = new float4(0, 0, 0, 1);
        
        if(! scene.bvhNodes[0].TryHitTriangle(scene, out TriangleHitInfo surfaceHit, pathRay, math.INFINITY))
            return rayEnergy;

        brdfData.roughness = scene.materials[surfaceHit.materialIndex].roughness;
        brdfData.N = surfaceHit.normal;
        brdfData.V = -pathRay.direction;

        // this is not physically correct, but I'm not adding light coming from direct lights and bounces
        // on emissive materials and I consider them their own single source of photons
        if (scene.materials[surfaceHit.materialIndex].isEmissive)
        {
            rayEnergy += scene.materials[surfaceHit.materialIndex].color;
        }
        else
        {
            foreach (var lightIndex in scene.bvhNodes[surfaceHit.nodeIndex].validLightIndices)
            {
                brdfData.L = math.normalize(scene.lights[lightIndex].position - surfaceHit.position);
                    
                float brdf = BRDF.Get(brdfData);
                rayEnergy += brdf * scene.lights[lightIndex].GetColorContribution(surfaceHit, scene);
            }   
            
            if (depth < scene.settings.indirectBounces)
            {
                pathRay.direction = TraceFunctions.RandomDirectionInHemisphere(surfaceHit.normal, rand.NextFloat3());

                pathRay.origin = surfaceHit.position + surfaceHit.normal * 0.0001f;
                // this is a rough implementation that tries to contain bouncing rays inside the lobe expected by the specularity of this roughness
                float3 reflectionVector = math.reflect(surfaceHit.incomingDirection, surfaceHit.normal);
                pathRay.direction = math.normalize( math.lerp(reflectionVector, pathRay.direction, brdfData.roughness));

                brdfData.L = pathRay.direction;
                float brdf = BRDF.Get(brdfData);

                bool rouletteTermination = (brdf < 0.1f * depth && rand.NextFloat() < 0.5f);

                if (!rouletteTermination)
                {
                    rayEnergy += PathRay(ref brdfData, ref pathRay, ref rand, depth + 1);  
                    
                    if (depth == 0)
                        rayEnergy *= scene.settings.indirectPower;
                }
            }
        }

        

        return (rayEnergy * scene.materials[surfaceHit.materialIndex].color);
    }
}