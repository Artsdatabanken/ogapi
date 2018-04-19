namespace NinMemApi.GraphDb
{
    public class Edge : Element
    {
        public Edge(G g, int label, string id, Vertex inVertex, Vertex outVertex) : base(g, label, id.ToLower())
        {
            InVertex = inVertex;
            OutVertex = outVertex;
        }

        public Edge(G g, int label, string id) : this(g, label, id.ToLower(), null, null)
        {
        }

        public Vertex InVertex { get; set; }
        public Vertex OutVertex { get; set; }

        public Vertex In() => InVertex;

        public Vertex Out() => OutVertex;

        public Edge AddP(int id, object value)
        {
            AddProperty(id, value);

            return this;
        }
    }
}