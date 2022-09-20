namespace Niarru.Zodchy.Server.Data;

public struct DbParameter
{
    public string Name { get; }
    public object Value { get; }

    public DbParameter(string name, object value)
    {
        this.Name = name;
        this.Value = value;
    }
}