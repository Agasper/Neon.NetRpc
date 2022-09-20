using System.Data;
using Google.Protobuf;
using Neon.Logging;
using Neon.Util.Pooling;

namespace Niarru.Zodchy.Server.Data
{
    //Base datastore class for saving player's progress
    //where T is a protobuf message for profile data
    public abstract class DataStore<T> : IDataStore<T> where T : IMessage, new()
    {
        protected static ILogger logger = LogManager.Default.GetLogger("Datastore");

        readonly IMemoryManager memoryManager;

        public DataStore(IMemoryManager memoryManager)
        {
            this.memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
        }

        public abstract Task DbCheck();

        protected Task CreateDbTableQuery()
        {
            return ExecuteQueryAsync("CREATE TABLE player (id INTEGER PRIMARY KEY AUTOINCREMENT, token VARCHAR(64) UNIQUE, data DATA);");
        }

        protected abstract Task<DbExecutionInfo> ExecuteQueryAsync(string query, params DbParameter[] args);

        protected abstract Task<IReadOnlyList<T>> ReadDataAsync(string query, Func<IDataRecord, T> selector,
            params DbParameter[] args);

        //reads protobuf message from database record
        protected T ReaderToMessage(IDataRecord record, int columnIndex)
        {
            byte[] rentedArray = memoryManager.RentArray(2048);
            try
            {
                using (var ms = memoryManager.GetStream(Guid.NewGuid()))
                {
                    long offset = 0;
                    long read;
                    while ((read = record.GetBytes(columnIndex, offset, rentedArray, 0, rentedArray.Length)) > 0)
                    {
                        offset += read;
                        ms.Write(rentedArray, 0, (int) read);
                    }

                    ms.Position = 0;
                    T result = new T();
                    using (CodedInputStream cis = new CodedInputStream(ms))
                        result.MergeFrom(cis);

                    return result;
                }
            }
            finally
            {
                memoryManager.ReturnArray(rentedArray);
            }
        }

        //updating existing player
        public async Task Update(PlayerCredentials credentials, T data)
        {
            var executionInfo = await ExecuteQueryAsync("UPDATE player SET data = @data WHERE id = @id AND token = @token;",
                new DbParameter("id", credentials.Id),
                new DbParameter("token", credentials.Token),
                new DbParameter("data", data.ToByteArray())).ConfigureAwait(false);
            if (executionInfo.AffectedRows == 0) //if no player found throw an exception
                throw new PlayerNotFoundException(credentials.Id);
            
            logger.Info($"Player saved #{credentials.Id}");
        }

        //creates a new player record
        public async Task<PlayerCredentials> Create(T data)
        {
            //token is a "password" for the player record
            string token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

            var executionInfo = await ExecuteQueryAsync(
                "INSERT INTO player (token, data) VALUES (@token, @data);",
                new DbParameter("token", token),
                new DbParameter("data", data.ToByteArray())).ConfigureAwait(false);

            //returning new credentials
            logger.Info($"New player created #{executionInfo.LastInsertId}");
            return new PlayerCredentials(executionInfo.LastInsertId, token);
        }

        //loading a player for specified credentials
        public async Task<T> Load(PlayerCredentials credentials)
        {
            var characters = await ReadDataAsync(
                "SELECT data FROM player WHERE id = @id AND token = @token",
                (reader) => ReaderToMessage(reader, 0),
                new DbParameter("id", credentials.Id),
                new DbParameter("token", credentials.Token)).ConfigureAwait(false);

            if (characters.Count == 0) //if no player found throw an exception
                throw new PlayerNotFoundException(credentials.Id);
            
            logger.Info($"Player loaded #{credentials.Id}");
            return characters[0];
        }
    }
}
