﻿using System;
using System.Runtime.CompilerServices;
using Unity.Mathematics;

namespace CapyTracerCore.Core.Functions
{
    public static class TraceCast
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool RayHitsBounds(in RenderRay ray, in BoundsBox bounds)
        {
            float tmin = (bounds.min.x - ray.origin.x) / ray.direction.x;
            float tmax = (bounds.max.x - ray.origin.x) / ray.direction.x;

            if (tmin > tmax)
            {
                (tmin, tmax) = (tmax, tmin); // Swap
            }

            float tymin = (bounds.min.y - ray.origin.y) / ray.direction.y;
            float tymax = (bounds.max.y - ray.origin.y) / ray.direction.y;

            if (tymin > tymax)
            {
                (tymin, tymax) = (tymax, tymin); // Swap
            }

            if ((tmin > tymax) || (tymin > tmax))
            {
                return false;
            }

            if (tymin > tmin)
            {
                tmin = tymin;
            }

            if (tymax < tmax)
            {
                tmax = tymax;
            }

            float tzmin = (bounds.min.z - ray.origin.z) / ray.direction.z;
            float tzmax = (bounds.max.z - ray.origin.z) / ray.direction.z;

            if (tzmin > tzmax)
            {
                (tzmin, tzmax) = (tzmax, tzmin); // Swap
            }

            if ((tmin > tzmax) || (tzmin > tmax))
            {
                return false;
            }

            return true;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int FindQtyHittingNodes(in RenderRay ray)
        {
            int qty = 0;

            // foreach (KDNode kdNode in Scene.s_nodesTree)
            // {
            //     qty += kdNode.GetHittingNodesQty(ray);
            // }

            return qty;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TriangleHitInfo TryHitTriangle(in RenderRay ray, in RenderScene scene, float maxDistance, bool fast)
        {
            float minHitDist = math.INFINITY;
            TriangleHitInfo hitInfo = new TriangleHitInfo();

            foreach (var renderTriangle in scene.triangles)
            {
                TriangleHitInfo thisHit = IntersectRayWithTriangle(renderTriangle, ray, maxDistance);

                if (thisHit.hitFound && thisHit.distance < minHitDist)
                {
                    minHitDist = thisHit.distance;
                    hitInfo = thisHit;
                }
            }
            
            // foreach (KDNode kdNode in Scene.s_nodesTree)
            // {
            //     if (kdNode.TryHitTriangle(out TriangleHitInfo nodeHit, ray, maxDistance, fast))
            //     {
            //         if (fast)
            //             return true;
            //
            //         if (nodeHit.distance < minHitDist)
            //         {
            //             minHitDist = nodeHit.distance;
            //             foundHit = true;
            //             hitInfo = nodeHit;
            //         }
            //     }
            // }

            return hitInfo;
        }

        // https://www.scratchapixel.com/lessons/3d-basic-rendering/ray-tracing-rendering-a-triangle/moller-trumbore-ray-triangle-intersection.html
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TriangleHitInfo IntersectRayWithTriangle(in RenderTriangle triangle, in RenderRay ray, float maxDistance)
        {
            TriangleHitInfo triangleHitInfo = new TriangleHitInfo();
            triangleHitInfo.hitFound = false;
            triangleHitInfo.incomingDirection = ray.direction;
            triangleHitInfo.distance = math.INFINITY;

            if (math.dot(ray.direction, triangle.faceNormal) >= 0)
                return triangleHitInfo;
            
            float3 pVec = math.cross(ray.direction, triangle.p0p2);
            float det = math.dot(triangle.p0p1, pVec);
        
            if (math.abs(det) < math.FLT_MIN_NORMAL)
                return triangleHitInfo;
        
            float invDet = 1f / det;

            float3 tVec = ray.origin - triangle.posA;
            float u = math.dot(tVec, pVec) * invDet;
        
            if (u < 0 || u > 1)
                return triangleHitInfo;
        
            float3 qVec = math.cross(tVec, triangle.p0p1);
        
            float v = math.dot(ray.direction, qVec) * invDet;
        
            if (v < 0 || (u + v) > 1)
                return triangleHitInfo;
        
            float distance = math.dot(triangle.p0p2, qVec) * invDet;
        
            if (distance <= 0 || distance > maxDistance)
                return triangleHitInfo;
        
            triangleHitInfo.hitFound = distance > 0;
            triangleHitInfo.distance = distance;
            triangleHitInfo.position = ray.origin + ray.direction * (triangleHitInfo.distance - 0.0001f);
            triangleHitInfo.materialIndex = triangle.materialIndex;

            triangleHitInfo.normal = triangle.normalA * ( 1 - u - v) + triangle.normalB * u + triangle.normalC * v;
            triangleHitInfo.normal = math.normalize(triangleHitInfo.normal);
        
            return triangleHitInfo;
        }
    }
}