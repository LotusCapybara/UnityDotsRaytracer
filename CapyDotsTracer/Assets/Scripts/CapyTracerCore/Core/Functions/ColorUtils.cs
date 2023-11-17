using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core
{
    public static class ColorUtils
    {    
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte[] ColorsToBytes(in float4[] colors)
        {
            byte[] pixelData = new byte[colors.Length * 4]; // Each pixel requires 4 bytes (R, G, B, A)

            for (int i = 0; i < colors.Length; i++)
            {
                pixelData[i * 4] = (byte)(MathF.Min(1f, colors[i].x) * 255);
                pixelData[i * 4 + 1] = (byte)(MathF.Min(1f, colors[i].y) * 255);
                pixelData[i * 4 + 2] = (byte)(MathF.Min(1f, colors[i].z) * 255);
                pixelData[i * 4 + 3] = (byte)(255);
            }

            return pixelData;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 ACESFilter(float4 x)
        {
            float a = 2.51f;
            float b = 0.03f;
            float c = 2.43f;
            float d = 0.59f;
            float e = 0.14f;
            return math.saturate((x * (a * x + b)) / (x * (c * x + d) + e));
        }
    }
}