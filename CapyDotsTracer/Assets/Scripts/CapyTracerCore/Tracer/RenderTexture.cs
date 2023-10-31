using CapyTracerCore.Core;
using Unity.Mathematics;

namespace CapyTracerCore
{
    public class RenderTexture
    {
        public float4[] colors;
        public int width;
        public int height;

        private int _totalSize;

        public RenderTexture(int width, int height)
        {
            this.width = width;
            this.height = height;

            _totalSize = width * height;
            colors = new float4[_totalSize];

            for (int i = 0; i < _totalSize; i++)
            {
                colors[i] = new float4(0, 0, 0, 1);
            }
        }
    }
}