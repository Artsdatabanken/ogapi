using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace NinMemApi.Data.Cache
{
    public class LRUCache<K, V>
    {
        private int _capacity;
        private Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>> _cacheMap = new Dictionary<K, LinkedListNode<LRUCacheItem<K, V>>>();
        private LinkedList<LRUCacheItem<K, V>> _lruList = new LinkedList<LRUCacheItem<K, V>>();

        public LRUCache(int capacity)
        {
            _capacity = capacity;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public V Get(K key)
        {
            if (_cacheMap.TryGetValue(key, out var node))
            {
                V value = node.Value.Value;
                _lruList.Remove(node);
                _lruList.AddLast(node);
                return value;
            }

            return default(V);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public bool ContainsKey(K key)
        {
            return _cacheMap.ContainsKey(key);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Add(K key, V val)
        {
            if (_cacheMap.Count >= _capacity)
            {
                RemoveFirst();
            }

            LRUCacheItem<K, V> cacheItem = new LRUCacheItem<K, V>(key, val);
            LinkedListNode<LRUCacheItem<K, V>> node = new LinkedListNode<LRUCacheItem<K, V>>(cacheItem);
            _lruList.AddLast(node);
            _cacheMap.Add(key, node);
        }

        private void RemoveFirst()
        {
            LinkedListNode<LRUCacheItem<K, V>> node = _lruList.First;
            _lruList.RemoveFirst();

            _cacheMap.Remove(node.Value.Key);
        }
    }

    public class LRUCacheItem<K, V>
    {
        public LRUCacheItem(K k, V v)
        {
            Key = k;
            Value = v;
        }

        public K Key { get; set; }
        public V Value { get; set; }
    }
}