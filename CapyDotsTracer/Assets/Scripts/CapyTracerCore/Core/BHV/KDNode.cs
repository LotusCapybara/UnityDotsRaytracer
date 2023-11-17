using System.Collections.Generic;
using CapyTracerCore.Core.Functions;

namespace CapyTracerCore.Core
{

    public class KDNode
    {
        public BoundsBox bounds;
        public KDNode childA;
        public KDNode childB;
        public int depth;
        public bool isLeaf;
        public List<RenderTriangle> triangles;
        public int qtyTriangles;

        public KDNode(BoundsBox bounds)
        {
            this.bounds = bounds;
            childA = null;
            childB = null;
            depth = 0;
            triangles = new List<RenderTriangle>();
            qtyTriangles = 0;
            isLeaf = true;
        }

        public void FinishGeneration()
        {
            if (isLeaf)
            {
                if (qtyTriangles == 0)
                    return;

                bounds = new BoundsBox();
                foreach (var renderTriangle in triangles)
                {
                    bounds.ExpandWithTriangle(renderTriangle);
                }
            }
            else
            {
                childA.FinishGeneration();
                childB.FinishGeneration();

                bounds = new BoundsBox();
                bounds.ExpandWithBounds(childA.bounds);
                bounds.ExpandWithBounds(childB.bounds);
            }
        }

        public void GetAllNodes(List<KDNode> nodes)
        {
            nodes.Add(this);

            if (!isLeaf)
            {
                childA.GetAllNodes(nodes);
                childB.GetAllNodes(nodes);
            }
        }


        public void TryInsertTriangle(in RenderTriangle triangle, in TracerSettings settings)
        {
            if (!bounds.IsPointInside(triangle.centerPoint))
                return;

            KDNode targetNode = null;
            KDNode currentNode = this;

            while (targetNode == null)
            {
                if (currentNode.isLeaf)
                {
                    targetNode = currentNode;
                }
                else
                {
                    currentNode = currentNode.childA.bounds.IsPointInside(triangle.centerPoint)
                        ? currentNode.childA
                        : currentNode.childB;
                }
            }

            targetNode.triangles.Add(triangle);
            targetNode.qtyTriangles++;
            
            if (targetNode.qtyTriangles >= settings.bvhTrianglesToExpand && targetNode.depth < settings.bvhMaxDepth)
            {
                var sah = targetNode.GetBestAxisToSplit();

                var newBounds = targetNode.bounds.GetSplit(sah.Item1, sah.Item2);

                targetNode.childA = new KDNode(newBounds.Item1);
                targetNode.childA.depth = targetNode.depth + 1;

                targetNode.childB = new KDNode(newBounds.Item2);
                targetNode.childB.depth = targetNode.depth + 1;

                foreach (var renderTriangle in targetNode.triangles)
                {
                    if (targetNode.childA.bounds.IsPointInside(renderTriangle.centerPoint))
                    {
                        targetNode.childA.triangles.Add(renderTriangle);
                        targetNode.childA.qtyTriangles++;
                    }
                    else
                    {
                        targetNode.childB.triangles.Add(renderTriangle);
                        targetNode.childB.qtyTriangles++;
                    }
                }
                
                
                
                targetNode.qtyTriangles = 0;
                targetNode.triangles = null;
                targetNode.isLeaf = false;
            }
        }

        public int GetTotalPartitionsQty()
        {
            int qty = 1;

            if (!isLeaf)
            {
                qty += childA.GetTotalPartitionsQty();
                qty += childB.GetTotalPartitionsQty();
            }

            return qty;
        }

        public int GetHittingNodesQty(in RenderRay ray)
        {
            int qty = 0;

            if (!TraceCast.RayHitsBounds(ray, bounds))
                return qty;

            qty++;
            if (!isLeaf)
            {
                qty += childA.GetHittingNodesQty(ray);
                qty += childB.GetHittingNodesQty(ray);
            }

            return qty;
        }

        private (int, float) GetBestAxisToSplit()
        {
            var score0 = GetAxisSplitScore(0);
            var score1 = GetAxisSplitScore(1);
            var score2 = GetAxisSplitScore(2);

            int axisToUse = 0;
            float bestScore = score0.Item1;
            float ratioToUse = score0.Item2;

            if (score1.Item1 < bestScore)
            {
                axisToUse = 1;
                bestScore = score1.Item1;
                ratioToUse = score1.Item2;
            }

            if (score2.Item1 < bestScore)
            {
                axisToUse = 2;
                ratioToUse = score2.Item2;
            }

            return (axisToUse, ratioToUse);
        }

        // splitting by using SAH: surface area heuristic
        // this means that based on the existing triangles in the volume, you consider the area of the triangles
        // on each axis and their possible splits, and heuristically come up with a value of what axis and what
        // split would mean better distribution of ray hits.
        // (float, float) = (bestScore, bestRatio)   score is the score for this axis, with the bestRatio (at what position of the axis should be split)
        // then outside this function, you check what axis had the best score, and use that one, with the given split position (the ratio)
        private (float, float) GetAxisSplitScore(int axis)
        {
            float bestScore = 9999f;
            float bestRatio = 0f;

            int qtySplits = 10;
            float axisSize = bounds.max[axis] - bounds.min[axis];
            float splitStep = axisSize / (qtySplits);


            for (int i = 1; i < qtySplits; i++)
            {
                float splitCenter = bounds.min[axis] + i * splitStep;

                float qty1 = 0;
                float qty2 = 0;

                foreach (var renderTriangle in triangles)
                {
                    if (renderTriangle.centerPoint[axis] <= splitCenter)
                    {
                        qty1++;
                    }
                    else
                    {
                        qty2++;
                    }
                }

                float p1 = (1f / qtySplits) * i;
                float p2 = 1f - p1;

                float score = 10f + p1 * 15 * qty1 + p2 * 15 * qty2;

                if (score < bestScore)
                {
                    bestScore = score;
                    bestRatio = p1;
                }
            }

            return (bestScore, bestRatio);
        }


    }
}