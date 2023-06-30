namespace Neon.Networking.Udp.Messages
{
    enum MessageType : byte
    {
        ConnectReq,
        ConnectResp,
        Ping,
        Pong,
        DisconnectReq,
        DisconnectResp,
        ExpandMTURequest,
        ExpandMTUSuccess,
        DeliveryAck,
        UserData
    }
}