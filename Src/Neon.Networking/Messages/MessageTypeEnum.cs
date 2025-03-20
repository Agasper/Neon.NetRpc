namespace Neon.Networking.Messages
{
    enum MessageTypeEnum : byte
    {
        UserData = 0,
        KeepAliveRequest = 1,
        KeepAliveResponse = 2,
        HandshakeRequest = 3,
        HandshakeResponse = 4,
    }
}