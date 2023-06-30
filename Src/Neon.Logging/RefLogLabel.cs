namespace Neon.Logging
{
    public delegate object Format<T>(T obj);

    public delegate object Format();

    /// <summary>
    ///     A lazy log label for meta information
    /// </summary>
    public class RefLogLabel
    {
        readonly Format _format;

        public RefLogLabel(Format format)
        {
            _format = format;
        }

        public override string ToString()
        {
            object f = _format();
            if (f == null)
                return "null";
            return f.ToString();
        }
    }

    /// <summary>
    ///     A lazy log label for meta information
    /// </summary>
    public class RefLogLabel<T>
    {
        readonly Format<T> _format;
        public T obj;

        public RefLogLabel(T obj, Format<T> format)
        {
            this.obj = obj;
            _format = format;
        }

        public override string ToString()
        {
            object f = _format(obj);
            if (f == null)
                return "null";
            return f.ToString();
        }
    }
}