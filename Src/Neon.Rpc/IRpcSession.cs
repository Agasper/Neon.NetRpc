using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Rpc
{
    public interface IRpcSession
    {
        object Tag { get; set; }
        bool IsClosed { get; }

        bool Close();

        Task ExecuteAsync(int methodIdentity);
        Task ExecuteAsync(string methodIdentity);
        Task ExecuteAsync(int methodIdentity, ExecutionOptions options);
        Task ExecuteAsync(string methodIdentity, ExecutionOptions options);
        Task ExecuteAsync<A>(int methodIdentity, A arg);
        Task ExecuteAsync<A>(string methodIdentity, A arg);
        Task ExecuteAsync<A>(int methodIdentity, A arg, ExecutionOptions options);
        Task ExecuteAsync<A>(string methodIdentity, A arg, ExecutionOptions options);
        Task<R> ExecuteAsync<R>(int methodIdentity);
        Task<R> ExecuteAsync<R>(string methodIdentity);
        Task<R> ExecuteAsync<R>(int methodIdentity, ExecutionOptions options);
        Task<R> ExecuteAsync<R>(string methodIdentity, ExecutionOptions options);
        Task<R> ExecuteAsync<R, A>(int methodIdentity, A arg);
        Task<R> ExecuteAsync<R, A>(string methodIdentity, A arg);
        Task<R> ExecuteAsync<R, A>(int methodIdentity, A arg, ExecutionOptions options);
        Task<R> ExecuteAsync<R, A>(string methodIdentity, A arg, ExecutionOptions options);
        Task Send(int methodIdentity);
        Task Send(string methodIdentity);
        Task Send(int methodIdentity, SendingOptions sendingOptions);
        Task Send(string methodIdentity, SendingOptions sendingOptions);
        Task Send<T>(int methodIdentity, T arg);
        Task Send<T>(string methodIdentity, T arg);
        Task Send<T>(int methodIdentity, T arg, SendingOptions sendingOptions);
        Task Send<T>(string methodIdentity, T arg, SendingOptions sendingOptions);
    }
}