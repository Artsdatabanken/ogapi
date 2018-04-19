using NinMemApi.Data.Elements;
using NinMemApi.Data.Elements.Properties;
using NinMemApi.GraphDb;
using NinMemApi.GraphDb.Labels;
using System.Collections.Generic;

namespace NinMemApi.Data
{
    public class StatTreeBuilder
    {
        private readonly G _g;
        private readonly CodeSearch _codeSearch;

        public StatTreeBuilder(G g, CodeSearch codeSearch)
        {
            _g = g;
            _codeSearch = codeSearch;
        }

        public StatTreeNode Build(string codes, string bbox, string rootNodeId)
        {
            var bboxResult = !string.IsNullOrWhiteSpace(bbox) ? _codeSearch.SearchByBbox(bbox) : new HashSet<string>();

            var groupedSearchCodes = _codeSearch.GetGroupedSearchCodes(codes);

            var rootVertex = _g.V(rootNodeId);

            var rootNode = new StatTreeNode(rootVertex.Id, rootVertex.Name);

            if (rootVertex.Parent != null)
            {
                rootNode.Parent = new StatTreeNode(rootVertex.Parent.Id, rootVertex.Parent.Name);
            }

            var topNodes = new Dictionary<string, StatTreeNode>();

            topNodes.Add(rootNode.Code, rootNode);

            foreach (var child in rootVertex.In(EL.Child))
            {
                var node = new StatTreeNode(child.Id, child.Name);
                node.Parent = rootNode;
                rootNode.Children.Add(child.Id, node);
                topNodes.Add(node.Code, node);
            }

            int pushed = 0;

            foreach (var pair in groupedSearchCodes)
            {
                var hashSet = new HashSet<string>();

                foreach (var code in pair.Value)
                {
                    var vertex = _g.V(code);

                    var stack = new Stack<Vertex>();
                    stack.Push(vertex);
                    var processedVertices = new HashSet<string>();

                    while (stack.Count > 0)
                    {
                        vertex = stack.Pop();

                        if (!processedVertices.Contains(vertex.Id) && IsRelevant(bboxResult, vertex))
                        {
                            Count(topNodes, vertex);

                            processedVertices.Add(vertex.Id);
                        }
                        else
                        {
                            var outVertices = vertex.Out(EL.In);

                            foreach (var outVertex in outVertices)
                            {
                                if (!processedVertices.Contains(outVertex.Id) && IsRelevant(bboxResult, outVertex))
                                {
                                    Count(topNodes, outVertex);

                                    processedVertices.Add(outVertex.Id);
                                }
                            }
                        }

                        var children = vertex.In(EL.Child);

                        foreach (var child in children)
                        {
                            stack.Push(child);
                            pushed++;
                        }
                    }
                }
            }

            return rootNode;
        }

        private static bool IsRelevant(HashSet<string> bboxResult, Vertex vertex)
        {
            return (vertex.Label == VL.NA || vertex.Label == VL.T) && (bboxResult.Count == 0 || bboxResult.Contains(vertex.Id));
        }

        private static void Count(Dictionary<string, StatTreeNode> topNodes, Vertex vertex)
        {
            if (vertex.Label == VL.NA)
            {
                double area = vertex.Value<double>(NAP.Area);

                var inEdges = vertex.InE(EL.In);

                foreach (var inEdge in inEdges)
                {
                    var connectedVertex = inEdge.Out();
                    var node = FindLowestAncestorNode(connectedVertex, topNodes);

                    if (node != null)
                    {
                        node.CountNatureArea(vertex.Id, area);
                    }
                }
            }
            else if (vertex.Label == VL.T)
            {
                var node = FindLowestAncestorNode(vertex, topNodes);

                if (node != null)
                {
                    node.CountTaxon(vertex.Id);
                }

                var inVertices = vertex.In(EL.In);

                foreach (var inVertex in inVertices)
                {
                    node = FindLowestAncestorNode(inVertex, topNodes);

                    if (node != null)
                    {
                        node.CountTaxon(vertex.Id);
                    }
                }
            }
        }

        private static StatTreeNode FindLowestAncestorNode(Vertex vertex, IDictionary<string, StatTreeNode> topNodes)
        {
            var current = vertex;

            while (current != null)
            {
                if (topNodes.ContainsKey(current.Id))
                {
                    var node = topNodes[current.Id];

                    if (current != vertex)
                    {
                        node.HasDescendants = true;
                    }

                    return node;
                }

                current = current.Parent;
            }

            return null;
        }
    }

    public class StatTreeNode
    {
        private readonly HashSet<string> _countedTaxons;
        private readonly HashSet<string> _countedNatureAreas;

        public StatTreeNode(string code, string name)
        {
            Code = code;
            Name = name;
            Children = new Dictionary<string, StatTreeNode>();
            _countedTaxons = new HashSet<string>();
            _countedNatureAreas = new HashSet<string>();
        }

        public string Code { get; set; }
        public string Name { get; set; }
        public int TaxonCount => _countedTaxons.Count;
        public int NatureAreaCount => _countedNatureAreas.Count;
        public double Area { get; set; }
        public IDictionary<string, StatTreeNode> Children { get; set; }
        public StatTreeNode Parent { get; set; }
        public bool HasDescendants { get; set; }

        public void CountTaxon(string code)
        {
            if (_countedTaxons.Contains(code))
            {
                return;
            }

            _countedTaxons.Add(code);

            if (Parent != null)
            {
                Parent.CountTaxon(code);
            }
        }

        public void CountNatureArea(string code, double area)
        {
            if (_countedNatureAreas.Contains(code))
            {
                return;
            }

            Area += area;

            _countedNatureAreas.Add(code);

            if (Parent != null)
            {
                Parent.CountNatureArea(code, area);
            }
        }
    }
}