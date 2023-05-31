using System;

namespace Neon.Logging
{
    public class LoggingNameFilter : IEquatable<LoggingNameFilter>
    {
        public string Name { get; }
        public LogSeverity Severity { get; }
        
        public LoggingNameFilter(string name, LogSeverity severity)
        {
            Name = name;
            Severity = severity;
        }

        public bool Equals(LoggingNameFilter other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Name == other.Name;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((LoggingNameFilter) obj);
        }

        public override int GetHashCode()
        {
            return (Name != null ? Name.GetHashCode() : 0);
        }
    }
}