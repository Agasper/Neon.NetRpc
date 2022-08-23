using System.Collections;
using System.Collections.Generic;
using Neon.Logging.Handlers;

namespace Neon.Logging
{
    public class LoggingHandlers : IList<ILoggingHandler>//, IReadOnlyList<ILoggingHandler>
    {
        List<ILoggingHandler> list;

        public int Count => list.Count;

        public LoggingHandlers()
        {
            this.list = new List<ILoggingHandler>();
        }
        
        public IEnumerator<ILoggingHandler> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(params ILoggingHandler[] handlers)
        {
            list.AddRange(handlers);
        }

        public void Add(ILoggingHandler item)
        {
            list.Add(item);
        }

        public void Clear()
        {
            list.Clear();
        }

        public bool Contains(ILoggingHandler item)
        {
            return list.Contains(item);
        }

        public void CopyTo(ILoggingHandler[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        public bool Remove(ILoggingHandler item)
        {
            return list.Remove(item);
        }

        int ICollection<ILoggingHandler>.Count => list.Count;
        public bool IsReadOnly { get; } = false;

        public int IndexOf(ILoggingHandler item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, ILoggingHandler item)
        {
            list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            list.RemoveAt(index);
        }

        public ILoggingHandler this[int index]
        {
            get => list[index];
            set => list[index] = value;
        }

        // int IReadOnlyCollection<ILoggingHandler>.Count => list.Count;
    }
}