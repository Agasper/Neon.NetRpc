using Neon.Rpc.Serialization;

namespace Neon.Rpc
{
    /// <summary>
    /// Interface for custom serialization messages
    /// </summary>
    public interface INeonMessage
    {
        /// <summary>
        /// Writes Neon message to the RPC message
        /// </summary>
        /// <param name="message">RPC message</param>
        void WriteTo(IRpcMessage message);
        /// <summary>
        /// Reads Neon message from the RPC message
        /// </summary>
        /// <param name="message">RPC message</param>
        void MergeFrom(IRpcMessage message);
    }
}
