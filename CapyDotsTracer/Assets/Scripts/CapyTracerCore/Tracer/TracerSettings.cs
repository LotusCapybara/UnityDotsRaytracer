namespace CapyTracerCore.Core
{
    public enum EDiffuseMode : byte
    {
        Lambert, OrenNayar
    }

    public enum ESpecularMode
    {
        Phong, BlinnPhong, CookTorrence
    }
    
    public struct TracerSettings
    {
        public int width;
        public int height;
        public int maxIterations;
        public int indirectBounces;
        
        public int bvhMaxDepth;
        public int bvhTrianglesToExpand;
        
        public float indirectPower;

        public EDiffuseMode diffuseMode;
        public ESpecularMode specularMode;
    }
}