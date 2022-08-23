namespace Neon.Networking.Udp.Messages
{
    public enum MessageType : byte
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
