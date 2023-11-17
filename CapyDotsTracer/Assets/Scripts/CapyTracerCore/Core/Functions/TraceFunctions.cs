using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core.Functions
{
    public static class TraceFunctions
    {
       
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float3 RandomDirectionInHemisphere(in float3 normal, in float3 randomValues) 
        {
            float3 randomDir = math.normalize(new float3(
                randomValues.x * 2f - 1f, 
                randomValues.y * 2f - 1f,  
                randomValues.z * 2f - 1f)
            );
            if (math.dot(randomDir, normal) <= 0)
                randomDir *= -1f;
            
            return randomDir;
        }
    }
}