using System;
using System.Collections;
using System.Collections.Generic;

namespace Neon.Logging
{
    public class LoggingNameFilters : IList<LoggingNameFilter>
    {
        /// <summary>
        ///     Default severity for all the names that are not in the filters
        /// </summary>
        public LogSeverity Default { get; set; }

        readonly List<LoggingNameFilter> _filters;

        LogSeverity _defaultFilter;

        /// <summary>
        ///     Creates a new LoggingNameFilters with no filters and default severity TRACE
        /// </summary>
        public LoggingNameFilters()
        {
            Default = LogSeverity.TRACE;
            _filters = new List<LoggingNameFilter>();
        }

        /// <summary>
        ///     Creates a new LoggingNameFilters with no filters
        /// </summary>
        /// <param name="defaultFilter">Default severity</param>
        public LoggingNameFilters(LogSeverity defaultFilter) : this()
        {
            _defaultFilter = defaultFilter;
        }

        /// <summary>
        ///     Creates a new LoggingNameFilters with the specified filters
        /// </summary>
        /// <param name="defaultFilter">Default severity</param>
        /// <param name="filters">Filters</param>
        public LoggingNameFilters(LogSeverity defaultFilter, params LoggingNameFilter[] filters) : this(defaultFilter)
        {
            _filters = new List<LoggingNameFilter>();
            foreach (LoggingNameFilter filter in filters) 
                _filters.Add(filter);
        }

        public IEnumerator<LoggingNameFilter> GetEnumerator()
        {
            return _filters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        ///     Adds a new filter
        /// </summary>
        /// <param name="item">filter</param>
        public void Add(LoggingNameFilter item)
        {
            _filters.Add(item);
        }

        /// <summary>
        ///     Clears all the filters
        /// </summary>
        public void Clear()
        {
            _filters.Clear();
        }

        public bool Contains(LoggingNameFilter item)
        {
            return _filters.Contains(item);
        }

        public void CopyTo(LoggingNameFilter[] array, int arrayIndex)
        {
            _filters.CopyTo(array, arrayIndex);
        }

        public bool Remove(LoggingNameFilter item)
        {
            return _filters.Remove(item);
        }

        public int Count => _filters.Count;
        public bool IsReadOnly => false;

        public int IndexOf(LoggingNameFilter item)
        {
            return _filters.IndexOf(item);
        }

        public void Insert(int index, LoggingNameFilter item)
        {
            _filters.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _filters.RemoveAt(index);
        }

        public LoggingNameFilter this[int index]
        {
            get => _filters[index];
            set => _filters[index] = value;
        }

        public bool IsFiltered(ILogger logger, LogSeverity severity)
        {
            if (severity < Default)
                return true;

            for (var i = 0; i < _filters.Count; i++)
            {
                LoggingNameFilter filter = _filters[i];
                if (logger.Name.StartsWith(filter.Name, StringComparison.InvariantCulture) && severity < filter.Severity)
                    return true;
            }

            return false;
        }
    }
}