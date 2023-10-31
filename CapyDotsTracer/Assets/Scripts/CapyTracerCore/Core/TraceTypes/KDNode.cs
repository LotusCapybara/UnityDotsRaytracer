// using System;
// using CapyTracerCore.Core.Functions;
//
// namespace CapyTracerCore.Core
// {
//     public class KDNode
//     {
//         private BoundsBox _bounds;
//         private KDNode _childA;
//         private KDNode _childB;
//         private int _depth;
//         private bool _isLeaf;
//         private RenderTriangle[] _triangles;
//         private int _qtyTriangles;
//
//         public KDNode(BoundsBox bounds)
//         {
//             _bounds = bounds;
//             _childA = null;
//             _childB = null;
//             _depth = 0;
//             _triangles = Array.Empty<RenderTriangle>();
//             _qtyTriangles = 0;
//             _isLeaf = true;
//         }
//
//         public void FinishGeneration()
//         {
//             if (_isLeaf)
//             {
//                 if (_qtyTriangles == 0)
//                     return;
//
//                 _bounds = new BoundsBox();
//                 foreach (var renderTriangle in _triangles)
//                 {
//                     _bounds.ExpandWithTriangle(renderTriangle);
//                 }
//             }
//             else
//             {
//                 _childA.FinishGeneration();
//                 _childB.FinishGeneration();
//
//                 _bounds = new BoundsBox();
//                 _bounds.ExpandWithBounds(_childA._bounds);
//                 _bounds.ExpandWithBounds(_childB._bounds);
//             }
//         }
//
//
//         public void TryInsertTriangle(in RenderTriangle triangle)
//         {
//             if (_isLeaf)
//             {
//                 if (_bounds.IsPointInside(triangle.centerPoint))
//                 {
//                     Array.Resize(ref _triangles, _qtyTriangles + 1);
//                     _triangles[_qtyTriangles] = triangle;
//                     _qtyTriangles++;
//
//                     if (_depth < TracerSettings.KDTREE_MAXDEPTH &&
//                         _qtyTriangles >= TracerSettings.KDTREE_MAXTRIANGLES_PER_LEVEL)
//                     {
//                         var sah = GetBestAxisToSplit();
//
//                         var newBounds = _bounds.GetSplit(sah.Item1, sah.Item2);
//
//                         _childA = new KDNode(newBounds.Item1);
//                         _childA._depth = _depth + 1;
//
//                         _childB = new KDNode(newBounds.Item2);
//                         _childB._depth = _depth + 1;
//
//                         foreach (var renderTriangle in _triangles)
//                         {
//                             _childA.TryInsertTriangle(renderTriangle);
//                             _childB.TryInsertTriangle(renderTriangle);
//                         }
//
//                         _qtyTriangles = 0;
//                         _triangles = null;
//                         _isLeaf = false;
//                     }
//                 }
//             }
//             else
//             {
//                 _childA.TryInsertTriangle(triangle);
//                 _childB.TryInsertTriangle(triangle);
//             }
//         }
//
//         public int GetTotalPartitionsQty()
//         {
//             int qty = 1;
//
//             if (!_isLeaf)
//             {
//                 qty += _childA.GetTotalPartitionsQty();
//                 qty += _childB.GetTotalPartitionsQty();
//             }
//
//             return qty;
//         }
//
//         public int GetHittingNodesQty(in RenderRay ray)
//         {
//             int qty = 0;
//
//             if (!TraceCast.RayHitsBounds(ray, _bounds))
//                 return qty;
//
//             qty++;
//             if (!_isLeaf)
//             {
//                 qty += _childA.GetHittingNodesQty(ray);
//                 qty += _childB.GetHittingNodesQty(ray);
//             }
//
//             return qty;
//         }
//
//         public bool TryHitTriangle(out TriangleHitInfo hitInfo, in RenderRay ray, float maxDistance, bool returnAtAny)
//         {
//             bool foundHit = false;
//             hitInfo = new TriangleHitInfo();
//
//             if (_isLeaf && _qtyTriangles == 0)
//                 return false;
//
//             if (!TraceCast.RayHitsBounds(ray, _bounds))
//                 return false;
//
//             if (_isLeaf)
//             {
//                 for (int t = 0; t < _qtyTriangles; t++)
//                 {
//                     TriangleHitInfo tHitInfo = TraceCast.IntersectRayWithTriangle(_triangles[t], ray, maxDistance, returnAtAny))
//                     {
//                         if (returnAtAny)
//                         {
//                             return true;
//                         }
//
//                         maxDistance = tHitInfo.distance;
//                         hitInfo = tHitInfo;
//                         foundHit = true;
//                     }
//                 }
//             }
//             else
//             {
//                 var didHitA = _childA.TryHitTriangle(out TriangleHitInfo hitInfoA, ray, maxDistance, returnAtAny);
//
//                 if (didHitA && returnAtAny)
//                 {
//                     return true;
//                 }
//
//                 var didHitB = _childB.TryHitTriangle(out TriangleHitInfo hitInfoB, ray, maxDistance, returnAtAny);
//
//                 if (didHitB && returnAtAny)
//                 {
//                     return true;
//                 }
//
//                 foundHit = didHitA || didHitB;
//
//                 if (didHitA && didHitB)
//                 {
//                     hitInfo = hitInfoA.distance < hitInfoB.distance ? hitInfoA : hitInfoB;
//                 }
//                 else if (didHitA)
//                 {
//                     hitInfo = hitInfoA;
//                 }
//                 else
//                 {
//                     hitInfo = hitInfoB;
//                 }
//
//             }
//
//             return foundHit;
//         }
//
//         private (int, float) GetBestAxisToSplit()
//         {
//             var score0 = GetAxisSplitScore(0);
//             var score1 = GetAxisSplitScore(1);
//             var score2 = GetAxisSplitScore(2);
//
//             int axisToUse = 0;
//             float bestScore = score0.Item1;
//             float ratioToUse = score0.Item2;
//
//             if (score1.Item1 < bestScore)
//             {
//                 axisToUse = 1;
//                 bestScore = score1.Item1;
//                 ratioToUse = score1.Item2;
//             }
//
//             if (score2.Item1 < bestScore)
//             {
//                 axisToUse = 2;
//                 ratioToUse = score2.Item2;
//             }
//
//             return (axisToUse, ratioToUse);
//         }
//
//         private (float, float) GetAxisSplitScore(int axis)
//         {
//             float bestScore = 9999f;
//             float bestRatio = 0f;
//
//             int qtySplits = 10;
//             float axisSize = _bounds.max[axis] - _bounds.min[axis];
//             float splitStep = axisSize / (qtySplits);
//
//
//             for (int i = 1; i < qtySplits; i++)
//             {
//                 float splitCenter = _bounds.min[axis] + i * splitStep;
//
//                 float qty1 = 0;
//                 float qty2 = 0;
//
//                 foreach (var renderTriangle in _triangles)
//                 {
//                     if (renderTriangle.centerPoint[axis] <= splitCenter)
//                     {
//                         qty1++;
//                     }
//                     else
//                     {
//                         qty2++;
//                     }
//                 }
//
//                 float p1 = (1f / qtySplits) * i;
//                 float p2 = 1f - p1;
//
//                 float score = 10f + p1 * 15 * qty1 + p2 * 15 * qty2;
//
//                 if (score < bestScore)
//                 {
//                     bestScore = score;
//                     bestRatio = p1;
//                 }
//             }
//
//             return (bestScore, bestRatio);
//         }
//
//
//     }
// }