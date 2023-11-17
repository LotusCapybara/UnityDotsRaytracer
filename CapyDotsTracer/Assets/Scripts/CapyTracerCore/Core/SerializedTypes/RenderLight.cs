using System.Runtime.InteropServices;
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

        private const float ONE_OVER_360 = 1f / 360f;

        public float4 GetColorContribution(in TriangleHitInfo hitInfo, in RenderScene scene)
        {
            ELightType lightType = (ELightType)type;

            float3 L = math.normalize(position - hitInfo.position);

            if (lightType == ELightType.Directional)
            {
                L = -forward;
            }
            
            
            float NdotL = math.dot(hitInfo.normal, L);
            
            if (NdotL <= 0)
                return new float4(0, 0, 0, 1);

            RenderRay ray = new RenderRay
            {
                direction = L,
                origin = hitInfo.position + L * 0.01f
            };
            
            float dist = math.distance(position, hitInfo.position);
            
            // shadow ray
            if(scene.bvhNodes[0].TryHitTriangleFast(scene, ray, dist))
                return new float4(0, 0, 0, 1);

            float power = intensity;
                        
            switch (lightType)
            {
                case ELightType.Point:
                    power = intensity / (dist * dist);
                    break;
                case ELightType.Spot:

                    float dotToOuter = math.dot(-L, forward);
                    float spotAngleFactor = 1f - angle * ONE_OVER_360 ;

                    if (dotToOuter <= spotAngleFactor)
                        return 0;
                    float angleDecay = math.saturate((dotToOuter - spotAngleFactor) / (1f - spotAngleFactor));
                    power = intensity * angleDecay / (dist * dist); 
                    break;
            }

            return color * power;
        }
        
    }
}