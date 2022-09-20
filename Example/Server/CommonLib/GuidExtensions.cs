using Google.Protobuf;

namespace Neon.ServerExample;

public static class GuidExtensions
{
    public static ByteString ToByteString(this Guid guid)
    {
        return ByteString.CopyFrom(guid.ToByteArray());
    }
    
    public static Guid ToGuid(this ByteString byteString)
    {
        return new Guid(byteString.ToByteArray());
    }
}