using System.Collections.Generic;
using System.Linq;

namespace Trees
{
    public class Trie<T>
    {
        private readonly TrieNode<T> _root;
        private readonly IDictionary<(T value, string field), int> _lengths;

        public Trie()
        {
            _root = new TrieNode<T>('^', 0, null);
            _lengths = new Dictionary<(T value, string field), int>();
        }

        public TrieNode<T> Prefix(string s)
        {
            var currentNode = _root;
            var result = currentNode;

            foreach (var c in s)
            {
                currentNode = currentNode.FindChildNode(c);

                if (currentNode == null)
                {
                    break;
                }

                result = currentNode;
            }

            return result;
        }

        public TrieNode<T> FindNode(string s)
        {
            var currentNode = _root;

            foreach (var c in s)
            {
                currentNode = currentNode.FindChildNode(c);

                if (currentNode == null)
                {
                    return null;
                }
            }

            return currentNode;
        }

        public T[] Search(string s)
        {
            string[] terms = GetTerms(s);

            HashSet<(T value, string field)> lastValues = null;

            IDictionary<string, HashSet<(T value, string field)>> termValues = new Dictionary<string, HashSet<(T value, string field)>>();

            foreach (var term in terms)
            {
                var node = FindNode(term);

                if (node == null)
                {
                    return new T[0];
                }

                HashSet<T> toCompare = null;

                if (lastValues != null)
                {
                    toCompare = new HashSet<T>();

                    foreach (var value in lastValues)
                    {
                        toCompare.Add(value.value);
                    }
                }

                lastValues = node.AllValues(toCompare);

                foreach (var value in lastValues)
                {
                    if (!termValues.TryGetValue(term, out var valueSet))
                    {
                        valueSet = new HashSet<(T value, string field)>();
                        termValues.Add(term, valueSet);
                    }

                    valueSet.Add(value);
                }
            }

            if (lastValues == null)
            {
                return new T[0];
            }

            var valueResultset = new HashSet<T>();

            foreach (var value in lastValues)
            {
                valueResultset.Add(value.value);
            }

            var weightedValues = new Dictionary<T, double>();

            foreach (var kvp in termValues)
            {
                string term = kvp.Key;
                var valueSet = kvp.Value;

                foreach (var value in valueSet)
                {
                    if (!valueResultset.Contains(value.value))
                    {
                        continue;
                    }

                    double weight = (double)term.Length / _lengths[value];

                    if (!weightedValues.ContainsKey(value.value))
                    {
                        weightedValues.Add(value.value, 0);
                    }

                    weightedValues[value.value] += weight;
                }
            }

            return weightedValues.OrderByDescending(wv => wv.Value).Select(wv => wv.Key).ToArray();
        }

        public bool IsInIndex(string s)
        {
            string[] terms = GetTerms(s);

            foreach (string term in terms)
            {
                var node = FindNode(term);

                if (!(node != null && node.Depth == term.Length && node.FindChildNode('$') != null))
                {
                    return false;
                }
            }

            return true;
        }

        private static string[] GetTerms(string s)
        {
            return TrieStringHelper.Parse(s, false);
        }

        public void InsertRange(List<string> items, T value, string field)
        {
            for (int i = 0; i < items.Count; i++)
            {
                Insert(items[i], value, field);
            }
        }

        public void Insert(string s, T value, string field)
        {
            var terms = TrieStringHelper.Parse(s);

            AddLength(s, value, field);

            foreach (var term in terms)
            {
                InsertPrivate(term, value, field);
            }
        }

        private void AddLength(string s, T value, string field)
        {
            var termsWithoutSuffixes = GetTerms(s);

            int length = GetLength(termsWithoutSuffixes);

            var tuple = (value, field);

            if (!_lengths.ContainsKey(tuple))
            {
                _lengths.Add(tuple, 0);
            }

            _lengths[tuple] += length;
        }

        private static int GetLength(string[] termsWithoutSuffixes)
        {
            return termsWithoutSuffixes.Sum(t => t.Length);
        }

        private void InsertPrivate(string term, T value, string field)
        {
            var commonPrefix = Prefix(term);
            var current = commonPrefix;

            for (var i = current.Depth; i < term.Length; i++)
            {
                var newNode = new TrieNode<T>(term[i], current.Depth + 1, current);
                current.AddChild(newNode);
                current = newNode;
            }

            var leafNode = current.FindChildNode('$');

            if (leafNode != null)
            {
                leafNode.AddValue(value, field);
                return;
            }
            else
            {
                var node = new TrieNode<T>('$', current.Depth + 1, current);
                node.AddValue(value, field);

                current.AddChild(node);
            }
        }

        // Not possible to delete as nodes are shared with other entries, rebuild the trie when necessary
        //public void Delete(string s)
        //{
        //    if (IsInIndex(s))
        //    {
        //        string[] terms = GetTerms(s);

        //        foreach (string term in terms)
        //        {
        //            var node = Prefix(term).FindChildNode('$');

        //            while (node.IsLeaf())
        //            {
        //                var parent = node.Parent;

        //                // TODO: Ensure not messing up other entries
        //                parent.DeleteChildNode(node.Character);
        //                node = parent;
        //            }
        //        }
        //    }
        //}
    }

    public class TrieValue<T>
    {
        public TrieValue(T value, string field)
        {
            Value = value;
            Field = field;
        }

        public T Value { get; set; }
        public string Field { get; set; }
    }
}