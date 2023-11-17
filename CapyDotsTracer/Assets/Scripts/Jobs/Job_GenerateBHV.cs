using System.Collections.Generic;
using CapyTracerCore.Core;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace Jobs
{
    public struct Job_GenerateBHV : IJob
    {
        public RenderScene scene;
        public NativeArray<StackBVHNode> outNodes;

        public void Execute()
        {
            // first I create a BVH tree using a KDNode which is a heap based implementation
            // each node has pointers to the children. The thing is that to be used in the Burst jobs
            // I need to pass this to a value type array. This is common too with GPU data structures,
            // you'll find that's common to do this same process to pass kd trees to the GPU.
            KDNode heapNodeRoot = new KDNode(scene.bounds);

            for(int i = 0; i < scene.triangles.Length; i++)
            {
                heapNodeRoot.TryInsertTriangle(scene.triangles[i], scene.settings);
            }
            
            // this is required, it's a little bit of boiler plate of my implementation
            // what this step does is to go through non-leaf nodes and adjust the size to the size
            // of the children. This is done because we might need to contract the nodes, and that helps
            // on the performance size. Let's say you have triangles that don't utilize the whole volume of a 
            // node, then by shrinking it you will have more rays that won't hit  unnecessarily the node.
            heapNodeRoot.FinishGeneration();
            
            List<KDNode> heapNodes = new List<KDNode>();
            heapNodeRoot.GetAllNodes(heapNodes);

            // the next steps create the NativeArray of value type stack nodes.
            // also, it sorts the NativeArray of triangles to be ordered in the same way their indices
            // are sorted inside the nodes. Basically, once this finishes, the array of triangles will have
            // "sections" of triangles and each section contains the triangles of a single node.
            // This favor locality and speeds up the process thanks to the CPU cache mechanisms.
            // Triangle sare going to be utilized by algorithms that go through them (check code like
            // StackBVHNode.TryHitTriangle for an example. If you keep the RenderTriangle data compact, the cache
            // line would include multiple triangles at once, and since you are linearly iterating it, the chances
            // of the next triangle of being already in the cache increase, compared with having a non sorted array
            outNodes = new NativeArray<StackBVHNode>(heapNodes.Count, Allocator.Persistent);

            NativeArray<RenderTriangle> sortedTriangles = scene.triangles;

            int ti = 0;
            int ni = 0;
            
            foreach (var heapNode in heapNodes)
            {
                if (heapNode.isLeaf)
                {
                    int qtyT = heapNode.triangles.Count;
                    for(int t = 0; t < qtyT; t++)
                    {
                        var triangle = heapNode.triangles[t];
                        triangle.nodeIndex = ni;
                        sortedTriangles[ti++] = triangle;
                        heapNode.triangles[t] = triangle;
                    }
                }
                ni++;
            }

            ti = 0;
            for(int i = 0; i < outNodes.Length; i++)
            {
                KDNode heapNode = heapNodes[i];
                
                int qtyTriangles = heapNode.isLeaf ? heapNode.triangles.Count : 0;
                
                UnsafeList<int> triangleIndices = new UnsafeList<int>(qtyTriangles, Allocator.Persistent);

                for (int t = 0; t < qtyTriangles; t++)
                {
                    triangleIndices.Add( ti + t);
                }
                
                // start of light relevance checking
                // this is a pretty rough implementation I made for this example that pre passes data to optimize times.
                // In this example, we analyze leaf nodes and try to estimate what lights are relevant to the triangles
                // inside that node,that's it, what lights could directly impact those triangles, and we mask out
                // the ones that are not relevant
                // This is later on used on TraceFunctions.GetLitColorOnSurface. The result is that you avoid tons
                // of unnecessary ray to triangle casts for shadow rays, resulting in a considerable performance optimization 
                // there are other more advanced techniques for this that are also utilized for more than just optimization
                // (ie photon mapping, which helps to have faster and more accurate caustics, etc)
                UnsafeList<int> lightIndices = new UnsafeList<int>(0, Allocator.Persistent);

                if (heapNode.isLeaf)
                {

                    for (int l = 0; l < scene.lights.Length; l++)
                    {
                        RenderLight light = scene.lights[l];

                        if (heapNode.bounds.IsPointInside(light.position))
                        {
                            lightIndices.Add(l);
                            continue;
                        }
                        
                        if (heapNode.bounds.SquaredDistToPoint(light.position) > (light.range * light.range))
                            continue;

                        if (light.type == (int)ELightType.Point)
                        {
                            bool hasRelevantTriangles = false;
                            foreach (var triangle in heapNode.triangles)
                            {
                                if (math.dot(triangle.faceNormal, (triangle.centerPoint - light.position)) < 0)
                                {
                                    hasRelevantTriangles = true;
                                    break;
                                }
                            }

                            if (!hasRelevantTriangles)
                                continue;
                        }
                        else if (light.type == (int)ELightType.Spot)
                        {
                            bool hasRelevantTriangles = false;
                            foreach (var triangle in heapNode.triangles)
                            {
                                bool isInRightHemisphere =
                                    math.dot(triangle.posA - light.position, light.forward) > 0 &&
                                    math.dot(triangle.posB- light.position, light.forward) > 0 &&
                                    math.dot(triangle.posC - light.position, light.forward) > 0;

                                if (isInRightHemisphere)
                                {
                                    hasRelevantTriangles = true;
                                    break;
                                }
                            }

                            if (!hasRelevantTriangles)
                                continue;
                        }
                        
                        lightIndices.Add(l);
                    }
                }
                // end of light relevance checking

                StackBVHNode stackNode = new StackBVHNode
                {
                    bounds = heapNode.bounds,
                    depth = heapNode.depth,
                    isLeaf = heapNode.isLeaf,
                    childA = heapNodes.IndexOf(heapNode.childA),
                    childB = heapNodes.IndexOf(heapNode.childB),
                    startIndex = ti,
                    qtyTriangles = triangleIndices.Length,
                    validLightIndices = lightIndices
                };

                outNodes[i] = stackNode;

                ti += triangleIndices.Length;
            }

            scene.triangles = sortedTriangles;
        }
    }
}