using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CapyTracerCore.Core.Functions;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public enum ELightType
    {
        Spot = 0,
        Directional = 1,
        Point = 2
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct RenderLight
    {
        public float4 color;
        public float3 position;
        public float3 forward;
        public float range;
        public float intensity;
        public float angle;
        public int type;

        public float4 GetColorContribution(in TriangleHitInfo hitInfo, in RenderScene scene)
        {
            ELightType lightType = (ELightType)type;
            switch (lightType)
            {
                case ELightType.Point:
                    return ProcessAsPoint(hitInfo, scene);
            }

            return new float4(0, 0, 0, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private float4 ProcessAsPoint(in TriangleHitInfo hitInfo, in RenderScene scene)
        {
            
            
            float3 L = math.normalize(position - hitInfo.position);
            
            float NdotL = math.dot(hitInfo.normal, L);
            
            if (NdotL <= 0)
                return new float4(0, 0, 0, 1);
            
            float dist = math.distance(position, hitInfo.position);
            
            RenderRay ray = new RenderRay
            {
                direction = L,
                origin = hitInfo.position + L * 0.01f
            };
            
            
            
            TriangleHitInfo shadowHit = TraceCast.TryHitTriangle(ray, scene, dist, true);
            
            if (shadowHit.hitFound)
                return new float4(0, 0, 0, 1);

            
            float power = 0.1f * intensity * (1f - Math.Clamp(dist / range, 0f, 1f));
            
            float diffuseTerm = LightFunctions.GetLambert(hitInfo.normal, L) * power;
            float specularTerm = 0f;
            
            float surfaceRoughness = scene.materials[hitInfo.materialIndex].roughness;
            
            if (surfaceRoughness < 1f)
            {
                float3 H = L + hitInfo.incomingDirection;
                specularTerm = LightFunctions.GetBlinnPhong(H, hitInfo.normal, surfaceRoughness);
            }
            
            return color * (power * Math.Clamp(diffuseTerm + specularTerm, 0, 1));
        }
    }
}