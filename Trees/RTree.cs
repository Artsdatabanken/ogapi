using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Trees
{
    public class RTree<T>
    {
        private static readonly EqualityComparer<T> Comparer = EqualityComparer<T>.Default;

        private readonly int _maxEntries;
        private readonly int _minEntries;

        private RTreeNode<T> _root;

        public int Height => _root.Height;

        public RTree(int maxEntries = 9)
        {
            _maxEntries = Math.Max(4, maxEntries);
            _minEntries = (int)Math.Max(2, Math.Ceiling(_maxEntries * 0.4));

            Init();
        }

        private void Init()
        {
            _root = new RTreeNode<T> { IsLeaf = true, Height = 1 };
        }

        public void Clear()
        {
            Init();
        }

        public string ToJson(bool indented = true)
        {
            return JsonConvert.SerializeObject(_root, indented ? Formatting.Indented : Formatting.None);
        }

        public void FromJson(string json)
        {
            _root = JsonConvert.DeserializeObject<RTreeNode<T>>(json);
        }

        public RTree<T> Load(IEnumerable<RTreeNode<T>> nnnn)
        {
            var nodes = nnnn.ToList();

            if (nodes.Count < _minEntries)
            {
                foreach (var n in nodes)
                {
                    Insert(n);
                }

                return this;
            }

            // Recursively build the tree with the given data from stratch using OMT algorithm.
            var node = BuildOneLevel(nodes, 0, 0);

            if (_root.Children.Count == 0)
            {
                // Save as is if tree is empty.
                _root = node;
            }
            else if (_root.Height == node.Height)
            {
                // Split root if trees have the same height.
                SplitRoot(_root, node);
            }
            else
            {
                if (_root.Height < node.Height)
                {
                    // Swap trees if inserted one is bigger.
                    var tmpNode = _root;
                    _root = node;
                    node = tmpNode;
                }

                // Insert the small tree into the large tree at appropriate level.
                Insert(node, _root.Height - node.Height - 1);
            }

            return this;
        }

        private RTreeNode<T> BuildOneLevel(List<RTreeNode<T>> items, int level, int height)
        {
            RTreeNode<T> node;
            var N = items.Count;
            var M = _maxEntries;

            if (N <= M)
            {
                node = new RTreeNode<T> { IsLeaf = true, Height = 1 };
                node.Children.AddRange(items);
            }
            else
            {
                if (level == 0)
                {
                    // Target height of the bulk-loaded tree.
                    height = (int)Math.Ceiling(Math.Log(N) / Math.Log(M));

                    // Target number of root entries to maximize storage utilization.
                    M = (int)Math.Ceiling((double)N / Math.Pow(M, height - 1));

                    items.Sort(CompareNodesByMinX);
                }

                node = new RTreeNode<T> { Height = height };

                var N2 = (int)Math.Ceiling((double)N / M);
                var N1 = (int)(Math.Ceiling((double)N / M) * Math.Ceiling(Math.Sqrt(M)));

                var compare =
                    level % 2 == 1
                        ? new Comparison<RTreeNode<T>>(CompareNodesByMinX)
                        : new Comparison<RTreeNode<T>>(CompareNodesByMinY);

                // Split the items into M mostly square tiles.
                for (var i = 0; i < N; i += N1)
                {
                    int takeFromItems = (N - i) < N1 ? (N - i) : N1;
                    var slice = items.GetRange(i, takeFromItems);
                    slice.Sort(compare);

                    for (var j = 0; j < slice.Count; j += N2)
                    {
                        // Pack each entry recursively.
                        int takeFromSlice = (slice.Count - j) < N2 ? (slice.Count - j) : N2;
                        var childNode = BuildOneLevel(slice.GetRange(j, takeFromSlice), level + 1, height - 1);
                        node.Children.Add(childNode);
                    }
                }
            }

            RefreshBoundingBox(node);

            return node;
        }

        public IEnumerable<RTreeNode<T>> Search(BoundingBox bbox)
        {
            var node = _root;

            if (!bbox.Intersects(node.BoundingBox))
            {
                return Enumerable.Empty<RTreeNode<T>>();
            }

            var retval = new List<RTreeNode<T>>();
            var nodesToSearch = new Stack<RTreeNode<T>>();

            while (node != null)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    var childBbox = child.BoundingBox;

                    if (bbox.Intersects(childBbox))
                    {
                        if (node.IsLeaf)
                        {
                            retval.Add(child);
                        }
                        else if (bbox.Contains(childBbox))
                        {
                            Collect(child, retval);
                        }
                        else
                        {
                            nodesToSearch.Push(child);
                        }
                    }
                }

                node = nodesToSearch.TryPop();
            }

            return retval;
        }

        public bool Collides(BoundingBox bbox)
        {
            var node = _root;

            if (!bbox.Intersects(node.BoundingBox))
            {
                return false;
            }

            var nodesToSearch = new Stack<RTreeNode<T>>();

            while (node != null)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    var childBbox = child.BoundingBox;

                    if (bbox.Intersects(childBbox))
                    {
                        if (node.IsLeaf || bbox.Contains(childBbox))
                        {
                            return true;
                        }

                        nodesToSearch.Push(child);
                    }
                }

                node = nodesToSearch.TryPop();
            }

            return false;
        }

        public IEnumerable<RTreeNode<T>> All()
        {
            var node = _root;

            var retval = new List<RTreeNode<T>>();
            var nodesToProcess = new Stack<RTreeNode<T>>();

            while (node != null)
            {
                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];

                    if (node.IsLeaf)
                    {
                        retval.Add(child);
                    }
                    else
                    {
                        Collect(child, retval);
                    }
                }

                node = nodesToProcess.TryPop();
            }

            return retval;
        }

        private static void Collect(RTreeNode<T> node, List<RTreeNode<T>> result)
        {
            var nodesToSearch = new Stack<RTreeNode<T>>();

            while (node != null)
            {
                if (node.IsLeaf)
                {
                    result.AddRange(node.Children);
                }
                else
                {
                    foreach (var n in node.Children)
                    {
                        nodesToSearch.Push(n);
                    }
                }

                node = nodesToSearch.TryPop();
            }
        }

        public RTree<T> Insert(T data, BoundingBox bounds)
        {
            return Insert(new RTreeNode<T>(data, bounds));
        }

        public RTree<T> Insert(RTreeNode<T> item)
        {
            return Insert(item, _root.Height - 1);
        }

        private RTree<T> Insert(RTreeNode<T> item, int level)
        {
            var bbox = item.BoundingBox;
            var insertPath = new List<RTreeNode<T>>();

            // Find the best node for accommodating the item, saving all nodes along the path as well.
            var node = ChooseSubtree(bbox, _root, level, insertPath);

            // Put the item into the node.
            node.Children.Add(item);
            node.BoundingBox.Extend(bbox);

            // Split on node overflow; propagate upwards if necessary.
            while (level >= 0)
            {
                if (insertPath[level].Children.Count <= _maxEntries)
                {
                    break;
                }

                Split(insertPath, level);
                level--;
            }

            // Adjust bboxes along the insertion path.
            AdjutsParentBounds(bbox, insertPath, level);

            return this;
        }

        public RTree<T> Remove(RTreeNode<T> nodeToRemove)
        {
            var node = _root;
            var itemBbox = nodeToRemove.BoundingBox;

            var path = new Stack<RTreeNode<T>>();
            var indexes = new Stack<int>();

            var i = 0;
            var goingUp = false;
            RTreeNode<T> parent = null;

            // Depth-first iterative tree traversal.
            while (node != null || path.Count > 0)
            {
                if (node == null)
                {
                    // Go up.
                    node = path.TryPop();
                    parent = path.TryPeek();
                    i = indexes.TryPop();

                    goingUp = true;
                }

                if (node != null && node.IsLeaf)
                {
                    // Check current node.
                    var toRemove = node.Children.Where(n => Comparer.Equals(nodeToRemove.Data, n.Data)).ToArray();

                    if (toRemove.Length > 0)
                    {
                        // Items found, remove the items and condense tree upwards.
                        foreach (var item in toRemove)
                        {
                            node.Children.Remove(item);
                        }

                        path.Push(node);
                        CondenseNodes(path.ToArray());
                    }
                }

                if (!goingUp && !node.IsLeaf && node.BoundingBox.Contains(itemBbox))
                {
                    // Go down.
                    path.Push(node);
                    indexes.Push(i);
                    i = 0;
                    parent = node;
                    node = node.Children[0];
                }
                else if (parent != null)
                {
                    i++;
                    if (i == parent.Children.Count)
                    {
                        // End of list; will go up.
                        node = null;
                    }
                    else
                    {
                        // Go right.
                        node = parent.Children[i];
                        goingUp = false;
                    }
                }
                else
                {
                    node = null; // Nothing found.
                }
            }

            return this;
        }

        private void CondenseNodes(IList<RTreeNode<T>> path)
        {
            // Go through the path, removing empty nodes and updating bboxes.
            for (var i = path.Count - 1; i >= 0; i--)
            {
                if (path[i].Children.Count == 0)
                {
                    if (i == 0)
                    {
                        Init();
                    }
                    else
                    {
                        var siblings = path[i - 1].Children;
                        siblings.Remove(path[i]);
                    }
                }
                else
                {
                    RefreshBoundingBox(path[i]);
                }
            }
        }

        // Split overflowed node into two.
        private void Split(List<RTreeNode<T>> insertPath, int level)
        {
            var node = insertPath[level];
            var totalCount = node.Children.Count;

            ChooseSplitAxis(node, _minEntries, totalCount);

            var newNode = new RTreeNode<T> { Height = node.Height };
            var splitIndex = ChooseSplitIndex(node, _minEntries, totalCount);

            newNode.Children.AddRange(node.Children.GetRange(splitIndex, node.Children.Count - splitIndex));
            node.Children.RemoveRange(splitIndex, node.Children.Count - splitIndex);

            if (node.IsLeaf) newNode.IsLeaf = true;

            RefreshBoundingBox(node);
            RefreshBoundingBox(newNode);

            if (level > 0)
            {
                insertPath[level - 1].Children.Add(newNode);
            }
            else
            {
                SplitRoot(node, newNode);
            }
        }

        private void SplitRoot(RTreeNode<T> node, RTreeNode<T> newNode)
        {
            // Split root node.
            _root = new RTreeNode<T>
            {
                Children = { node, newNode },
                Height = node.Height + 1
            };

            RefreshBoundingBox(_root);
        }

        private static int ChooseSplitIndex(RTreeNode<T> node, int minEntries, int totalCount)
        {
            var minOverlap = Int32.MaxValue;
            var minArea = Int32.MaxValue;
            int index = 0;

            for (var i = minEntries; i <= totalCount - minEntries; i++)
            {
                var bbox1 = SumChildBounds(node, 0, i);
                var bbox2 = SumChildBounds(node, i, totalCount);

                var overlap = IntersectionArea(bbox1, bbox2);
                var area = bbox1.Area + bbox2.Area;

                // Choose distribution with minimum overlap.
                if (overlap < minOverlap)
                {
                    minOverlap = overlap;
                    index = i;

                    minArea = area < minArea ? area : minArea;
                }
                else if (overlap == minOverlap)
                {
                    // Otherwise choose distribution with minimum area.
                    if (area < minArea)
                    {
                        minArea = area;
                        index = i;
                    }
                }
            }

            return index;
        }

        private static RTreeNode<T> ChooseSubtree(BoundingBox bbox, RTreeNode<T> node, int level, List<RTreeNode<T>> path)
        {
            while (true)
            {
                path.Add(node);

                if (node.IsLeaf || path.Count - 1 == level) break;

                var minArea = Int32.MaxValue;
                var minEnlargement = Int32.MaxValue;

                RTreeNode<T> targetNode = null;

                for (var i = 0; i < node.Children.Count; i++)
                {
                    var child = node.Children[i];
                    var area = child.BoundingBox.Area;
                    var enlargement = CombinedArea(bbox, child.BoundingBox) - area;

                    // Choose entry with the least area enlargement.
                    if (enlargement < minEnlargement)
                    {
                        minEnlargement = enlargement;
                        minArea = area < minArea ? area : minArea;
                        targetNode = child;
                    }
                    else if (enlargement == minEnlargement)
                    {
                        // Otherwise choose one with the smallest area.
                        if (area < minArea)
                        {
                            minArea = area;
                            targetNode = child;
                        }
                    }
                }

                Debug.Assert(targetNode != null);
                node = targetNode;
            }

            return node;
        }

        private static int CombinedArea(BoundingBox what, BoundingBox with)
        {
            var minX1 = Math.Max(what.X1, with.X1);
            var minY1 = Math.Max(what.Y1, with.Y1);
            var maxX2 = Math.Min(what.X2, with.X2);
            var maxY2 = Math.Min(what.Y2, with.Y2);

            return (maxX2 - minX1) * (maxY2 - minY1);
        }

        private static int IntersectionArea(BoundingBox what, BoundingBox with)
        {
            var minX = Math.Max(what.X1, with.X1);
            var minY = Math.Max(what.Y1, with.Y1);
            var maxX = Math.Min(what.X2, with.X2);
            var maxY = Math.Min(what.Y2, with.Y2);

            return Math.Max(0, maxX - minX) * Math.Max(0, maxY - minY);
        }

        // Calculate node's bbox from bboxes of its children
        private static void RefreshBoundingBox(RTreeNode<T> node)
        {
            node.BoundingBox = SumChildBounds(node, 0, node.Children.Count);
        }

        private static BoundingBox SumChildBounds(RTreeNode<T> node, int startIndex, int endIndex)
        {
            var retval = new BoundingBox();

            for (var i = startIndex; i < endIndex; i++)
            {
                retval.Extend(node.Children[i].BoundingBox);
            }

            return retval;
        }

        private static void AdjutsParentBounds(BoundingBox bbox, List<RTreeNode<T>> path, int level)
        {
            // Adjust bboxes along the given tree path.
            for (var i = level; i >= 0; i--)
            {
                path[i].BoundingBox.Extend(bbox);
            }
        }

        // Sorts node children by the best axis for split.
        private static void ChooseSplitAxis(RTreeNode<T> node, int m, int M)
        {
            var xMargin = AllDistMargin(node, m, M, CompareNodesByMinX);
            var yMargin = AllDistMargin(node, m, M, CompareNodesByMinY);

            // If total distributions margin value is minimal for x, sort by minX,
            // otherwise it's already sorted by minY.
            if (xMargin < yMargin)
            {
                node.Children.Sort(CompareNodesByMinX);
            }
        }

        private static int CompareNodesByMinX(RTreeNode<T> a, RTreeNode<T> b)
        {
            return a.BoundingBox.X1.CompareTo(b.BoundingBox.X1);
        }

        private static int CompareNodesByMinY(RTreeNode<T> a, RTreeNode<T> b)
        {
            return a.BoundingBox.Y1.CompareTo(b.BoundingBox.Y1);
        }

        private static int AllDistMargin(RTreeNode<T> node, int m, int M, Comparison<RTreeNode<T>> compare)
        {
            node.Children.Sort(compare);

            var leftBBox = SumChildBounds(node, 0, m);
            var rightBBox = SumChildBounds(node, M - m, M);
            var margin = leftBBox.Margin + rightBBox.Margin;

            for (var i = m; i < M - m; i++)
            {
                var child = node.Children[i];
                leftBBox.Extend(child.BoundingBox);
                margin += leftBBox.Margin;
            }

            for (var i = M - m - 1; i >= m; i--)
            {
                var child = node.Children[i];
                rightBBox.Extend(child.BoundingBox);
                margin += rightBBox.Margin;
            }

            return margin;
        }
    }
}