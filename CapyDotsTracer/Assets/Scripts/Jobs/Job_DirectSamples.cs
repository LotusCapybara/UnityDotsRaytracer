using CapyTracerCore.Core;
using CapyTracerCore.Core.Functions;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct Job_DirectSamples : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<RenderRay> cameraRays;

    [ReadOnly]
    public RenderScene scene;

    [WriteOnly]
    public NativeArray<TriangleHitInfo> cameraHits;

    [WriteOnly]
    public NativeArray<float4> directColors;

    public void Execute(int index)
    {
        TriangleHitInfo hitInfo = TraceCast.TryHitTriangle(cameraRays[index], scene, math.INFINITY, false);
        cameraHits[index] = hitInfo;

        if (hitInfo.hitFound)
        {
            directColors[index] = TraceFunctions.GetLitColorOnSurface(hitInfo, scene);    
        }
        else
        {
            directColors[index] = new float4(1, 0, 0, 1);
        }
    }
}
