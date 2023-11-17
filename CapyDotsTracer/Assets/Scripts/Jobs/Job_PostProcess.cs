using CapyTracerCore.Core;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct Job_PostProcess : IJobParallelFor
{
    [ReadOnly]
    public NativeArray<float4> sampleColors;
    
    [WriteOnly]
    public NativeArray<float4> finalColors;

    public void Execute(int index)
    {
        finalColors[index] = ColorUtils.ACESFilter(sampleColors[index]);
    }
}
