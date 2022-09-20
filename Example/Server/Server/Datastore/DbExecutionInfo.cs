namespace Niarru.Zodchy.Server.Data;

public struct DbExecutionInfo
{
    public int AffectedRows { get; }
    public long LastInsertId { get; }

    public DbExecutionInfo(int affectedRows, long lastInsertId)
    {
        this.AffectedRows = affectedRows;
        this.LastInsertId = lastInsertId;
    }
}