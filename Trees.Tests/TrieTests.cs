using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace Trees.Tests
{
    [TestClass]
    public class TrieTests
    {
        private readonly Dictionary<int, string> _data = new Dictionary<int, string>
        {
            { 1, "The quick brown fox jumps over the lazy dog." },
            { 2, "If it ain't broke, don't fix it." },
            { 3, "All good things must come to an end." },
            { 4, "The silver fox." }
        };

        [TestMethod]
        public void Test()
        {
            var trie = new Trie<int>();

            foreach (var kvp in _data)
            {
                trie.Insert(kvp.Value, kvp.Key, "whatever");
            }

            Assert.IsTrue(trie.IsInIndex("brown"));

            var ids = trie.Search("brown");

            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual(1, ids.First());

            ids = trie.Search("row");

            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual(1, ids.First());

            ids = trie.Search("ain't");

            Assert.AreEqual(1, ids.Length);
            Assert.AreEqual(2, ids.First());

            ids = trie.Search("fox");

            Assert.AreEqual(2, ids.Length);
            Assert.IsTrue(ids.Contains(1));
            Assert.IsTrue(ids.Contains(4));

            ids = trie.Search("notExists");

            Assert.AreEqual(ids.Length, 0);

            ids = trie.Search("foxe");

            Assert.AreEqual(ids.Length, 0);
        }
    }
}