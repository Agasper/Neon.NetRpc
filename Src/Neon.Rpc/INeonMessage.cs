using Neon.Rpc.Serialization;

namespace Neon.Rpc
{
    public interface INeonMessage
    {
        void WriteTo(IRpcMessage message);
        void MergeFrom(IRpcMessage message);
    }
}
