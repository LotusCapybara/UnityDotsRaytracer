using System.Runtime.InteropServices;
using CapyTracerCore.Core.Functions;
using Unity.Collections.LowLevel.Unsafe;

namespace CapyTracerCore.Core
{
    [StructLayout(LayoutKind.Sequential)]
    public struct StackBVHNode
    {
        public int depth;
        public BoundsBox bounds;
        public bool isLeaf;
        public int childA;
        public int childB;
        public int startIndex;
        public int qtyTriangles;
        
        // a potential alternative to this would be to use a single numeric variable as a bit mask
        // instead of storing all the indices. That would make this struct smaller and more predictable
        // with the downside of limiting the amount of lights you could have in a scene (depending on the byte size 
        // of the data type used). For instance, if you use an int, you would have max 32 lights in the scene.
        public UnsafeList<int> validLightIndices;

        // I have 2 implementations of triangle hit, one that allocates TriangleHitInfo to gather information of the hits
        // and another that only cares about checking if there is or not a hit 
        // I need to dive a bit more into C# and pointers and see how I can do this with a single implementation
        
        public bool TryHitTriangle(in RenderScene scene , out TriangleHitInfo hitInfo, in RenderRay ray, float maxDistance)
        {
            bool foundHit = false;
            hitInfo = new TriangleHitInfo();

            if (isLeaf && qtyTriangles == 0)
                return false;

            if (!TraceCast.RayHitsBounds(ray, bounds))
                return false;

            if (isLeaf)
            {
                float closestDist = maxDistance;
                
                for (int tIndex = startIndex; tIndex < (startIndex + qtyTriangles); tIndex++)
                {
                    TriangleHitInfo tHitInfo = TraceCast.IntersectRayWithTriangle(scene.triangles[tIndex], ray, closestDist);

                    if (tHitInfo.hitFound && tHitInfo.distance < closestDist)
                    {
                        closestDist = tHitInfo.distance;
                        hitInfo = tHitInfo;
                        foundHit = true;
                    }
                }
            }
            else
            {
                var didHitA = scene.bvhNodes[childA].TryHitTriangle(scene, out TriangleHitInfo hitInfoA, ray, maxDistance);
                var didHitB = scene.bvhNodes[childB].TryHitTriangle(scene, out TriangleHitInfo hitInfoB, ray, maxDistance);

                foundHit = didHitA || didHitB;

                if (didHitA && didHitB)
                {
                    hitInfo = hitInfoA.distance < hitInfoB.distance ? hitInfoA : hitInfoB;
                }
                else if (didHitA)
                {
                    hitInfo = hitInfoA;
                }
                else
                {
                    hitInfo = hitInfoB;
                }

            }

            return foundHit;
        }
        
        
        public bool TryHitTriangleFast(in RenderScene scene , in RenderRay ray, float maxDistance)
        {
            if (isLeaf && qtyTriangles == 0)
                return false;

            if (!TraceCast.RayHitsBounds(ray, bounds))
                return false;
            
            if (isLeaf)
            {
                for (int tIndex = startIndex; tIndex < (startIndex + qtyTriangles); tIndex++)
                {
                    if (TraceCast.FastIntersectRayWithTriangle(scene.triangles[tIndex], ray, maxDistance))
                    {
                        return true;
                    }
                }
            }
            else
            {
                var didHitA = scene.bvhNodes[childA].TryHitTriangleFast(scene, ray, maxDistance);
                var didHitB = scene.bvhNodes[childB].TryHitTriangleFast(scene, ray, maxDistance);

                return didHitA || didHitB;

            }

            return false;
        }
        
    }
}