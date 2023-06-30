using System.Threading.Tasks;
using Google.Protobuf;

namespace Neon.Rpc
{
    public interface IRpcSession
    {
        IRpcConnection Connection { get; }
        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync(string method);

        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync(string method, ExecutionOptions options);

        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync<A>(string method, A arg)  where A : IMessage;

        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync<A>(string method, A arg, ExecutionOptions options) where A : IMessage;

        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R>(string method) where R : IMessage<R>,new();

        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R>(string method, ExecutionOptions options) where R : IMessage<R>,new();
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R, A>(string method, A arg) where R : IMessage<R>,new() where A : IMessage;

        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R, A>(string method, A arg, ExecutionOptions options)where R : IMessage<R>,new() where A : IMessage;

        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send(string method);

        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send(string method, SendingOptions options);

        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send<T>(string method, T arg)where T : IMessage;

        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="method">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send<T>(string method, T arg, SendingOptions options) where T : IMessage;
    }
}