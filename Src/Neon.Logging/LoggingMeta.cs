using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Neon.Logging
{
    public class LoggingMeta : IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        Dictionary<string, object> meta;

        public static LoggingMeta Empty { get; } = new LoggingMeta();

        public LoggingMeta()
        {
            this.meta = new Dictionary<string, object>();
        }

        public LoggingMeta(int capacity)
        {
            this.meta = new Dictionary<string, object>(capacity);
        }

        public LoggingMeta(IReadOnlyDictionary<string, object> meta): this()
        {
            foreach (var pair in meta)
                this.meta.Add(pair.Key, pair.Value);
        }

        public LoggingMeta(params KeyValuePair<string, object>[] tags) : this()
        {
            foreach (var pair in tags)
                this.meta.Add(pair.Key, pair.Value);
        }

        public object this[string key]
        {
            get => meta[key];
            set => meta[key] = value;
        }

        public static LoggingMeta Merge(params LoggingMeta[] meta)
        {
            LoggingMeta newMeta = null;
            for(int i = 0; i< meta.Length; i++)
            {
                var m = meta[i];
                if (m.Count > 0)
                {
                    if (newMeta == null)
                        newMeta = new LoggingMeta();
                    
                    foreach (var pair in m)
                    {
                        newMeta[pair.Key] = pair.Value;
                    }
                }
            }

            return newMeta ?? LoggingMeta.Empty;
        }

        public LoggingMeta Copy()
        {
            return new LoggingMeta(this.meta);
        }

        public ICollection<string> Keys => meta.Keys;

        public ICollection<object> Values => meta.Values;

        public int Count => meta.Count;

        public bool IsReadOnly => false;

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => meta.Keys;

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => meta.Values;

        public void Add(string key, object value)
        {
            meta.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            meta.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            meta.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return (meta as IDictionary<string, object>).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return meta.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            (meta as IDictionary<string, object>).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return meta.GetEnumerator();
        }

        public bool Remove(string key)
        {
             return meta.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return (meta as IDictionary<string, object>).Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return meta.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return meta.GetEnumerator();
        }
    }
}
