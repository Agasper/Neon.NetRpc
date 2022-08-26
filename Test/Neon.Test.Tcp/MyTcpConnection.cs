using Neon.Networking.Messages;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Test.Util;

namespace Neon.Test.Tcp;

public class MyTcpConnection : TcpConnection
{
    public MyTcpConnection(TcpPeer peer) : base(peer)
    {
    }

    protected override void OnMessageReceived(MessageEventArgs args)
    {
        using (args)
        {
            string nickname = args.Message.ReadString();
            string message = args.Message.ReadString();
            logger.Info($"[{Parent}] [RECEIVED] {nickname}: {message}");

            if (!IsClientConnection)
                _ = SendChatMessage("Server", "Message accepted");
        }
        base.OnMessageReceived(args);
    }

    public async Task SendChatMessage(string nickname, string message)
    {
        using (RawMessage newMsg = Parent.CreateMessage())
        {
            newMsg.Write(nickname);
            newMsg.Write(message);
            await this.SendMessageAsync(newMsg);
            logger.Info($"[{Parent}] [SENT] {nickname}: {message}");
        }
    }
}