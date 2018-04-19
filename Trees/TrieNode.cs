using System.Collections.Generic;

namespace Trees
{
    public class TrieNode<T>
    {
        public char Character { get; set; }
        public HashSet<(T value, string field)> Values { get; set; }
        public Dictionary<char, TrieNode<T>> Children { get; set; }
        public TrieNode<T> Parent { get; set; }
        public int Depth { get; set; }

        public TrieNode(char character, int depth, TrieNode<T> parent)
        {
            Character = character;
            Values = new HashSet<(T value, string field)>();
            Children = new Dictionary<char, TrieNode<T>>();
            Depth = depth;
            Parent = parent;
        }

        public HashSet<(T value, string field)> AllValues(HashSet<T> toCompare, HashSet<(T value, string field)> values = null)
        {
            if (values == null)
            {
                values = new HashSet<(T value, string field)>();
            }

            foreach (var value in Values)
            {
                if (toCompare == null || toCompare.Contains(value.value))
                {
                    values.Add(value);
                }
            }

            foreach (var child in Children.Values)
            {
                child.AllValues(toCompare, values);
            }

            return values;
        }

        public bool IsLeaf()
        {
            return Children.Count == 0;
        }

        public void AddChild(TrieNode<T> child)
        {
            Children.Add(child.Character, child);
        }

        public void AddValue(T value, string field)
        {
            var key = (value, field);
            if (!Values.Contains(key))
            {
                Values.Add(key);
            }
        }

        public TrieNode<T> FindChildNode(char c)
        {
            Children.TryGetValue(c, out var node);

            return node;
        }

        //public void DeleteChildNode(char c)
        //{
        //    if (Children.ContainsKey(c))
        //    {
        //        Children.Remove(c);
        //    }
        //}
    }
}