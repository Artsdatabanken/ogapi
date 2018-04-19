using NinMemApi.GraphDb.Labels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.GraphDb
{
    public class Vertex : Element
    {
        private readonly static Vertex[] _emptyVertexResult = new Vertex[0];
        private readonly static Edge[] _emptyEdgeResult = new Edge[0];

        private readonly IDictionary<int, List<Edge>> _outEdges;
        private readonly IDictionary<int, List<Edge>> _inEdges;
        private readonly IDictionary<(int, int), HashSet<string>> _outEdgeVertexIds;

        public Vertex(G g, int label, string id) : base(g, label, id.ToLower())
        {
            _outEdges = new Dictionary<int, List<Edge>>();
            _inEdges = new Dictionary<int, List<Edge>>();
            _outEdgeVertexIds = new Dictionary<(int, int), HashSet<string>>();
        }

        public string Name { get; private set; }
        public Vertex Parent { get; private set; }

        public IEnumerable<Vertex> Out(int label)
        {
            if (!_outEdges.ContainsKey(label))
            {
                return _emptyVertexResult;
            }

            return _outEdges[label].Select(e => e.InVertex);
        }

        public IEnumerable<string> OutId(int label, int vertexLabel)
        {
            var key = (label, vertexLabel);

            if (!_outEdgeVertexIds.ContainsKey(key))
            {
                return new string[0];
            }

            return _outEdgeVertexIds[key];
        }

        public IEnumerable<Vertex> In(int label)
        {
            if (!_inEdges.ContainsKey(label))
            {
                return _emptyVertexResult;
            }

            return _inEdges[label].Select(e => e.OutVertex);
        }

        public IEnumerable<Edge> OutE(int label)
        {
            if (!_outEdges.ContainsKey(label))
            {
                return _emptyEdgeResult;
            }

            return _outEdges[label];
        }

        public IEnumerable<Edge> InE(int label)
        {
            if (!_inEdges.ContainsKey(label))
            {
                return _emptyEdgeResult;
            }

            return _inEdges[label];
        }

        public Vertex AddP(int id, object value)
        {
            AddProperty(id, value);

            if (id == P.Name)
            {
                Name = (string)value;
            }

            return this;
        }

        public Edge AddE(int label, Vertex to)
        {
            if (label == EL.Child && Parent != null)
            {
            }
            if (!_outEdges.TryGetValue(label, out var edges))
            {
                edges = new List<Edge>();

                _outEdges.Add(label, edges);
            }

            var key = (label, to.Label);

            if (!_outEdgeVertexIds.TryGetValue(key, out var edgeVertexIds))
            {
                edgeVertexIds = new HashSet<string>();

                _outEdgeVertexIds.Add(key, edgeVertexIds);
            }

            var edge = new Edge(_g, label, Guid.NewGuid().ToString().ToLower(), to, this);

            edges.Add(edge);
            edgeVertexIds.Add(to.Id);
            to.AddInE(edge);
            _g.AddE(edge);

            if (label == EL.Child)
            {
                Parent = to;
            }

            return edge;
        }

        private Edge AddInE(Edge edge)
        {
            if (!_inEdges.TryGetValue(edge.Label, out var edges))
            {
                edges = new List<Edge>();

                _inEdges.Add(edge.Label, edges);
            }

            edges.Add(edge);

            return edge;
        }
    }
}