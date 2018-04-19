using System.Collections.Generic;

namespace Trees
{
    public class RTreeNode<T>
    {
        private readonly List<RTreeNode<T>> _children;

        internal RTreeNode() : this(default(T), new BoundingBox())
        {
        }

        public RTreeNode(T data, BoundingBox bbox)
        {
            Data = data;
            BoundingBox = bbox;
            _children = new List<RTreeNode<T>>();
        }

        public T Data { get; set; }
        public BoundingBox BoundingBox { get; set; }

        public bool IsLeaf { get; set; }
        public int Height { get; set; }
        public List<RTreeNode<T>> Children => _children;
    }
}