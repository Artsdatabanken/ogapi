using System.Collections.Generic;
using System.Linq;

namespace NinMemApi.GraphDb
{
    public class G
    {
        private IDictionary<string, Vertex> _verticesById = new Dictionary<string, Vertex>();
        private IDictionary<string, Edge> _edgesById = new Dictionary<string, Edge>();
        private IDictionary<int, List<Vertex>> _verticesByLabel = new Dictionary<int, List<Vertex>>();
        private IDictionary<int, List<Edge>> _edgesByLabel = new Dictionary<int, List<Edge>>();

        public IEnumerable<Vertex> V()
        {
            return _verticesById.Values;
        }

        public Vertex V(string id)
        {
            id = id.ToLower();

            if (!HasV(id))
            {
                ThrowKeyNotFoundException(id);
            }

            return _verticesById[id];
        }

        public bool HasV(string id)
        {
            return _verticesById.ContainsKey(id.ToLower());
        }

        public bool TryGetV(string id, out Vertex v)
        {
            id = id.ToLower();

            return _verticesById.TryGetValue(id, out v);
        }

        public IEnumerable<Vertex> V(params string[] ids)
        {
            var nonExistingIds = ids.Where(id => !_verticesById.ContainsKey(id.ToLower())).ToArray();

            if (nonExistingIds.Length > 0)
            {
                ThrowKeyNotFoundException(nonExistingIds);
            }

            return ids.Select(id => _verticesById[id.ToLower()]);
        }

        public IEnumerable<Edge> E()
        {
            return _edgesById.Values;
        }

        public Edge E(string id)
        {
            id = id.ToLower();

            if (!_edgesById.ContainsKey(id))
            {
                ThrowKeyNotFoundException(id);
            }

            return _edgesById[id];
        }

        public Vertex AddV(int label, string id)
        {
            id = id.ToLower();

            var vertex = new Vertex(this, label, id);

            _verticesById.Add(id, vertex);

            if (!_verticesByLabel.TryGetValue(label, out var vertices))
            {
                vertices = new List<Vertex>();

                _verticesByLabel.Add(label, vertices);
            }

            vertices.Add(vertex);

            return vertex;
        }

        public Edge AddE(int label, string id)
        {
            id = id.ToLower();

            var edge = new Edge(this, label, id);

            return AddE(edge);
        }

        public Edge AddE(Edge edge)
        {
            _edgesById.Add(edge.Id, edge);

            if (!_edgesByLabel.TryGetValue(edge.Label, out var edges))
            {
                edges = new List<Edge>();

                _edgesByLabel.Add(edge.Label, edges);
            }

            edges.Add(edge);

            return edge;
        }

        private static void ThrowKeyNotFoundException(string id)
        {
            throw new KeyNotFoundException($"id {id} does not exist.");
        }

        private static void ThrowKeyNotFoundException(string[] ids)
        {
            throw new KeyNotFoundException($"ids {string.Join(", ", ids)} do not exist.");
        }
    }
}