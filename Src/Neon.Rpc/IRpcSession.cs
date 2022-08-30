using System.Threading.Tasks;
using Neon.Networking.Udp.Messages;

namespace Neon.Rpc
{
    public interface IRpcSession
    {
        /// <summary>
        /// User-defined tag
        /// </summary>
        object Tag { get; set; }
        /// <summary>
        /// Is session closed
        /// </summary>
        bool IsClosed { get; }

        /// <summary>
        /// Closes session, cause connection termination
        /// </summary>
        void Close();

        /// <summary>
        /// Executes remoting method identified by integer with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync(int methodIdentity);
        
        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync(string methodIdentity);
        
        /// <summary>
        /// Executes remoting method identified by integer with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync(int methodIdentity, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by string name with no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync(string methodIdentity, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync<A>(int methodIdentity, A arg);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync<A>(string methodIdentity, A arg);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync<A>(int methodIdentity, A arg, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and no result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task ExecuteAsync<A>(string methodIdentity, A arg, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by integer with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R>(int methodIdentity);
        
        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R>(string methodIdentity);
        
        /// <summary>
        /// Executes remoting method identified by integer with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R>(int methodIdentity, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by string name with result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R>(string methodIdentity, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R, A>(int methodIdentity, A arg);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R, A>(string methodIdentity, A arg);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R, A>(int methodIdentity, A arg, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument passed and result awaiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Execution options</param>
        /// <returns>A task that represents remote method completion</returns>
        Task<R> ExecuteAsync<R, A>(string methodIdentity, A arg, ExecutionOptions options);
        
        /// <summary>
        /// Executes remoting method identified by integer with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send(int methodIdentity);
        
        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send(string methodIdentity);
        
        /// <summary>
        /// Executes remoting method identified by integer with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send(int methodIdentity, SendingOptions options);
        
        /// <summary>
        /// Executes remoting method identified by string name with no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send(string methodIdentity, SendingOptions options);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send<T>(int methodIdentity, T arg);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send<T>(string methodIdentity, T arg);
        
        /// <summary>
        /// Executes remoting method identified by integer with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send<T>(int methodIdentity, T arg, SendingOptions options);
        
        /// <summary>
        /// Executes remoting method identified by string name with argument, but no completion waiting
        /// </summary>
        /// <param name="methodIdentity">Method identity</param>
        /// <param name="arg">Method argument</param>
        /// <param name="options">Sending options</param>
        /// <returns>A task that represents operation of RPC request sending</returns>
        Task Send<T>(string methodIdentity, T arg, SendingOptions options);
    }
}