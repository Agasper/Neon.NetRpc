using System.Data;
using System.Data.SQLite;
using System.Runtime.CompilerServices;
using Google.Protobuf;
using Neon.Util.Pooling;

namespace Niarru.Zodchy.Server.Data;

// SQLite datastore 
public class DataStoreSqlite<T> : DataStore<T> where T : IMessage, new()
{
    readonly string dbPath;
    
    public DataStoreSqlite(IMemoryManager memoryManager, string dbPath) : base(memoryManager)
    {
        this.dbPath = dbPath;
    }

    //Checking SQLite database file is exists and ready
    public override async Task DbCheck()
    {
        bool create = !File.Exists(dbPath);
        
        if (create)
        {
            //creating file, and database
            logger.Debug("Creating DB...");
            SQLiteConnection.CreateFile(dbPath);
            await CreateDbTableQuery();
        }
        else
        {
            logger.Debug("Vacuuming database...");
            await ExecuteQueryAsync("VACUUM;");
        }

        //checking its working
        await ExecuteQueryAsync("SELECT 1;");
    }
    
    //SQLite connection string
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    string GetConnectionString()
    {
        return $"Data Source={dbPath};Version=3;Pooling=True;Max Pool Size=100;";
    }

    //creates a new connection
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected async Task<SQLiteConnection> GetConnectionAsync()
    {
        var connection = new SQLiteConnection(GetConnectionString());
        await connection.OpenAsync().ConfigureAwait(false);
        return connection;
    }
    
    //executing query returning no rows
    protected override async Task<DbExecutionInfo> ExecuteQueryAsync(string query, params DbParameter[] args)
    {
        using (var connection = await GetConnectionAsync().ConfigureAwait(false))
        {
            logger.Debug(query);
            using (SQLiteCommand command = new SQLiteCommand(query, connection))
            {
                for (int i = 0; i < args.Length; i++)
                {
                    command.Parameters.Add(new SQLiteParameter(args[i].Name, args[i].Value));
                }

                int affectedRows = await command.ExecuteNonQueryAsync().ConfigureAwait(false);
                return new DbExecutionInfo(affectedRows, connection.LastInsertRowId);
            }
        }
    }

    //Executing query returning classes according to the selector
    protected override async Task<IReadOnlyList<T>> ReadDataAsync(string query, Func<IDataRecord, T> selector,
        params DbParameter[] args)
    {
        using (var connection = await GetConnectionAsync().ConfigureAwait(false))
        {
            using (SQLiteDataAdapter adapter = new SQLiteDataAdapter())
            {
                using (SQLiteCommand command = new SQLiteCommand(query, connection))
                {
                    for (int i = 0; i < args.Length; i++)
                    {
                        command.Parameters.Add(new SQLiteParameter(args[i].Name, args[i].Value));
                    }

                    using (var reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                    {
                        var items = new List<T>();
                        while (await reader.ReadAsync().ConfigureAwait(false))
                            items.Add(selector(reader));
                        return items;
                    }
                }
            }
        }
    }
}