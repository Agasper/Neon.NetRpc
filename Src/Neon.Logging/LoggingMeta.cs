using System.Collections;
using System.Collections.Generic;

namespace Neon.Logging
{
    public class LoggingMeta : IDictionary<string, object>, IReadOnlyDictionary<string, object>
    {
        /// <summary>
        ///     Returns an empty meta collection
        /// </summary>
        public static LoggingMeta Empty { get; } = new LoggingMeta();

        readonly Dictionary<string, object> _meta;

        public LoggingMeta()
        {
            _meta = new Dictionary<string, object>();
        }

        public LoggingMeta(int capacity)
        {
            _meta = new Dictionary<string, object>(capacity);
        }

        public LoggingMeta(IReadOnlyDictionary<string, object> meta) : this()
        {
            foreach (KeyValuePair<string, object> pair in meta)
                _meta.Add(pair.Key, pair.Value);
        }

        public LoggingMeta(params KeyValuePair<string, object>[] tags) : this()
        {
            foreach (KeyValuePair<string, object> pair in tags)
                _meta.Add(pair.Key, pair.Value);
        }

        public object this[string key]
        {
            get => _meta[key];
            set => _meta[key] = value;
        }

        public ICollection<string> Keys => _meta.Keys;

        public ICollection<object> Values => _meta.Values;

        public int Count => _meta.Count;

        public bool IsReadOnly => false;

        public void Add(string key, object value)
        {
            _meta.Add(key, value);
        }

        public void Add(KeyValuePair<string, object> item)
        {
            _meta.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _meta.Clear();
        }

        public bool Contains(KeyValuePair<string, object> item)
        {
            return (_meta as IDictionary<string, object>).Contains(item);
        }

        public bool ContainsKey(string key)
        {
            return _meta.ContainsKey(key);
        }

        public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
        {
            (_meta as IDictionary<string, object>).CopyTo(array, arrayIndex);
        }

        public IEnumerator<KeyValuePair<string, object>> GetEnumerator()
        {
            return _meta.GetEnumerator();
        }

        public bool Remove(string key)
        {
            return _meta.Remove(key);
        }

        public bool Remove(KeyValuePair<string, object> item)
        {
            return (_meta as IDictionary<string, object>).Remove(item);
        }

        public bool TryGetValue(string key, out object value)
        {
            return _meta.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _meta.GetEnumerator();
        }

        IEnumerable<string> IReadOnlyDictionary<string, object>.Keys => _meta.Keys;

        IEnumerable<object> IReadOnlyDictionary<string, object>.Values => _meta.Values;

        /// <summary>
        ///     Merging all the meta collections. If the key duplicates, the last one will be taken
        /// </summary>
        /// <param name="meta">Array of meta collections</param>
        /// <returns>Merged meta collection</returns>
        public static LoggingMeta Merge(params LoggingMeta[] meta)
        {
            LoggingMeta newMeta = null;
            for (var i = 0; i < meta.Length; i++)
            {
                LoggingMeta m = meta[i];
                if (m.Count > 0)
                {
                    if (newMeta == null)
                        newMeta = new LoggingMeta();

                    foreach (KeyValuePair<string, object> pair in m) newMeta[pair.Key] = pair.Value;
                }
            }

            return newMeta ?? Empty;
        }

        public LoggingMeta Copy()
        {
            return new LoggingMeta(_meta);
        }
    }
}