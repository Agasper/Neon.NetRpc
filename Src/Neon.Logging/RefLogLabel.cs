namespace Neon.Logging
{
    public delegate object Format<T>(T obj);
    public delegate object Format();

    /// <summary>
    /// A lazy log label for meta information
    /// </summary>
    public class RefLogLabel
    {
        Format format;
        
        public RefLogLabel(Format format)
        {
            this.format = format;
        }
        
        public override string ToString()
        {
            object f = format();
            if (f == null)
                return "null";
            else
                return f.ToString();
        }
    }
        
    /// <summary>
    /// A lazy log label for meta information
    /// </summary>
    public class RefLogLabel<T>
    {
        public T obj;
        Format<T> format;

        public RefLogLabel(T obj, Format<T> format)
        {
            this.obj = obj;
            this.format = format;
        }

        public override string ToString()
        {
            object f = format(obj);
            if (f == null)
                return "null";
            else
                return f.ToString();
        }
    }
}