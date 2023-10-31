using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class LightFunctions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetLambert(in float3 N, in float3 L)
        {
            return math.saturate(math.dot(N, L));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetBlinnPhong(in float3 H, in float3 N, float roughness)
        {
            float specDot = math.max(math.dot(N, H), 0);
            return math.pow(specDot, 64) * (1f - roughness);
        }
    }
}