using System.Collections;
using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public class LoggingHandlers : IList<ILoggingHandler> //, IReadOnlyList<ILoggingHandler>
    {
        public int Count => _list.Count;
        readonly List<ILoggingHandler> _list;

        public LoggingHandlers()
        {
            _list = new List<ILoggingHandler>();
        }

        public IEnumerator<ILoggingHandler> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(ILoggingHandler item)
        {
            _list.Add(item);
        }

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(ILoggingHandler item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(ILoggingHandler[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(ILoggingHandler item)
        {
            return _list.Remove(item);
        }

        int ICollection<ILoggingHandler>.Count => _list.Count;
        public bool IsReadOnly { get; } = false;

        public int IndexOf(ILoggingHandler item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, ILoggingHandler item)
        {
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public ILoggingHandler this[int index]
        {
            get => _list[index];
            set => _list[index] = value;
        }

        public void Add(params ILoggingHandler[] handlers)
        {
            _list.AddRange(handlers);
        }

        // int IReadOnlyCollection<ILoggingHandler>.Count => list.Count;
    }
}