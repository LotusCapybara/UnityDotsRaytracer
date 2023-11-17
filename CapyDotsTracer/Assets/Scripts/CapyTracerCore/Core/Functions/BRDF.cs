using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public struct BRDFData
    {
        public float roughness;
        public float3 L; // from surface to light
        public float3 V; // from surface to view
        public float3 N;
        public byte diffuseMode;
        public byte specularMode;
        
        public float3 H =>  math.normalize(V + L);
        public float NdotH => math.saturate(math.dot(N, H));
        public float VdotH => math.saturate(math.dot(V, H));
        public float NdotV => math.saturate(math.dot(N, V));
        public float NdotL => math.saturate(math.dot(N, L));
        
    }
    
    public static class BRDF
    {
        private const float ONE_OVER_PI = 1f / math.PI; 
        
        // the returning tupla is
        // (float, float) = (diffuse term, specular term)
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Get(in BRDFData data)
        {
            float fresnel = Fresnel_Schlick(data.H, data.V, data.NdotV);
            
            float diffuse = GetDiffuseTerm(data);
            float specular = GetSpecularTerm(data, fresnel);

            // (check here): https://www.scratchapixel.com/lessons/3d-basic-rendering/global-illumination-path-tracing/global-illumination-path-tracing-practical-implementation.html
            // (and here): https://boksajak.github.io/files/CrashCourseBRDF.pdf
            return math.saturate( math.lerp(diffuse, specular,fresnel) );
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetDiffuseTerm(in BRDFData data)
        {
            // diffuse terms usually include a ONE_OVER_PI
            // however, we use an implicit PDF = (N.L / PI) which would cancel that PI
            // it's actually also cancelling the N.L but I'm leaving it for now
            // since it looks better in this example and I'd need to improve the diffuse reflectance formula
            switch (data.diffuseMode)
            {
                case (byte) EDiffuseMode.Lambert:
                    return GetLambert(data);
                case (byte) EDiffuseMode.OrenNayar:
                    return GetOrenNayar(data);
            }

            return 0f;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetSpecularTerm(in BRDFData data, float fresnel)
        {
            switch (data.specularMode)
            {
                case (byte) ESpecularMode.BlinnPhong:
                    return GetBlinnPhong(data);
                case (byte) ESpecularMode.CookTorrence:
                    return GetCookTorrence(data, fresnel);
            }

            return 0f;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetLambert(in BRDFData data)
        {
            return math.saturate(math.dot(data.N, data.L));
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetOrenNayar(in BRDFData data)
        {
            float sigma = 0.7071067f * math.atan(data.roughness * data.roughness);

            float thetaV = math.acos(data.NdotV);
            float thetaL = math.acos(data.NdotL);

            float alpha = math.max(thetaV, thetaL);
            float beta = math.min(thetaV, thetaL);

            // Calculate cosine of azimuth angles difference - by projecting L and V onto plane defined by N. Assume L, V, N are normalized.
            float3 l = data.L - data.NdotL * data.N;
            float3 v = data.V - data.NdotV * data.N;
            float cosPhiDifference = math.dot(math.normalize(v), math.normalize(l));

            float sigma2 = sigma * sigma;
            float A = 1.0f - 0.5f * (sigma2 / (sigma2 + 0.33f));
            float B = 0.45f * (sigma2 / (sigma2 + 0.09f));

            float orenNayar = (A + B * math.max(0.0f, cosPhiDifference) * math.sin(alpha) * math.tan(beta));
            return math.saturate(math.dot(data.N, data.L)) *  orenNayar;
        }
        
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetBlinnPhong(in BRDFData data)
        {
            float specDot = math.max(math.dot(data.N, data.H), 0);
            return math.pow(specDot, 128) * (1f - data.roughness);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetCookTorrence(in BRDFData data, float F)
        {
            // Fresnel_Cook-Torrence BRDF
            
            float  G = Geometry_Smith(data.roughness, data.NdotV, data.NdotL);
            float  D = NormalDistributionGGX(data.NdotH, data.roughness);
            float denom = math.max(4.0f * data.NdotV * data.NdotL, 0.0001f);
        
            float specular = D * F * G / denom;
            return specular;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Fresnel_Schlick(float3 N, float3 V, float nDotV)
        {	
            return 0.5f + (1f - 0.5f) * math.pow(1.0f - math.max(0.0f, nDotV), 5.0f);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Geometry_Smith(float k, float nDotV, float nDotL)
        {	
            return  Geometry_Smiths_SchlickGGX(k, nDotV) * Geometry_Smiths_SchlickGGX(k, nDotL);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float Geometry_Smiths_SchlickGGX(float roughness, float dot)
        {	
            float k = math.pow((roughness + 1.0f), 2) / 8.0f;
            float NV = math.max(0.0f, dot);
            float denom = (NV * (1.0f - k) + k) + 0.0001f;

            return NV / denom;
        }
        
        // Trowbridge-Reitz GGX Distribution
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float NormalDistributionGGX(float NdotH, float roughness) 
        {	
            float a = roughness * roughness;
            float a2 = a * a;
            float nh2 = math.pow(NdotH, 2);
            float denom = (math.PI * math.pow((nh2 * (a2 - 1.0f) + 1.0f), 2));
            if (denom < math.EPSILON) return 1.0f;
            return a2 / denom;
        }
    }
}