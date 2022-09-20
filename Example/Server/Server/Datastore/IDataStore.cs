using Google.Protobuf;

namespace Niarru.Zodchy.Server.Data
{

    public interface IDataStore<T> where T : IMessage, new()
    {
        Task DbCheck();
        Task Update(PlayerCredentials credentials, T data);
        Task<PlayerCredentials> Create(T data);
        Task<T> Load(PlayerCredentials credentials);
    }

}