using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Neon.Logging
{
    public class LoggingNameFilters : IList<LoggingNameFilter>
    {
        /// <summary>
        /// Default severity for all the names that are not in the filters
        /// </summary>
        public LogSeverity Default { get; set; }
        
        LogSeverity defaultFilter;
        readonly List<LoggingNameFilter> filters;
        
        /// <summary>
        /// Creates a new LoggingNameFilters with no filters and default severity TRACE
        /// </summary>
        public LoggingNameFilters()
        {
        }

        public bool IsFiltered(ILogger logger, LogSeverity severity)
        {
            if (severity < Default)
                return true;
            
            for (int i = 0; i < filters.Count; i++)
            {
                var filter = filters[i];
                if (logger.Name.StartsWith(filter.Name, StringComparison.InvariantCulture))
                    return true;
            }

            return false;
        }
        
        /// <summary>
        /// Creates a new LoggingNameFilters with no filters
        /// </summary>
        /// <param name="defaultFilter">Default severity</param>
        public LoggingNameFilters(LogSeverity defaultFilter)
        {
            this.defaultFilter = defaultFilter;
            this.filters = new List<LoggingNameFilter>();
        }
        
        /// <summary>
        /// Creates a new LoggingNameFilters with the specified filters
        /// </summary>
        /// <param name="defaultFilter">Default severity</param>
        /// <param name="filters">Filters</param>
        public LoggingNameFilters(LogSeverity defaultFilter, params LoggingNameFilter[] filters)
        {
            this.defaultFilter = defaultFilter;
            this.filters = new List<LoggingNameFilter>();
            foreach (var filter in filters)
            {
                this.filters.Add(filter);
            }
        }
        
        public IEnumerator<LoggingNameFilter> GetEnumerator()
        {
            return filters.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Adds a new filter
        /// </summary>
        /// <param name="item">filter</param>
        public void Add(LoggingNameFilter item)
        {
            filters.Add(item);
        }

        /// <summary>
        /// Clears all the filters
        /// </summary>
        public void Clear()
        {
            filters.Clear();
        }
        
        public bool Contains(LoggingNameFilter item)
        {
            return filters.Contains(item);
        }
        
        public void CopyTo(LoggingNameFilter[] array, int arrayIndex)
        {
            filters.CopyTo(array, arrayIndex);
        }

        public bool Remove(LoggingNameFilter item)
        {
            return filters.Remove(item);
        }

        public int Count => filters.Count;
        public bool IsReadOnly => false;
        
        public int IndexOf(LoggingNameFilter item)
        {
            return filters.IndexOf(item);
        }

        public void Insert(int index, LoggingNameFilter item)
        {
            filters.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            filters.RemoveAt(index);
        }

        public LoggingNameFilter this[int index]
        {
            get => filters[index];
            set => filters[index] = value;
        }
    }
}