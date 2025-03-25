using Neon.Networking.Messages;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Crc32 = ICSharpCode.SharpZipLib.Checksum.Crc32;

namespace Neon.Test.Tcp;

public class MyTcpConnection : TcpConnection
{
    public int RecvBytes => _recvMessages.Sum(v => v.Length);
    
    List<byte[]> _recvMessages = new();
    List<byte[]> _sentMessages = new();
    
    public MyTcpConnection(TcpPeer peer) : base(peer)
    {
    }

    byte[] GenerateBytes(int length)
    {
        var bytes = new byte[length];
        for (int i = 0; i < length; i++)
        {
            bytes[i] = (byte) (i / 100);
        }

        return bytes;
    }

    protected override void OnMessageReceived(MessageEventArgs args)
    {
        using (args)
        {
            int len = args.Message.ReadInt32();
            _recvMessages.Add(args.Message.ReadBytes(len));
            _logger.Info($"[{Parent}] [RECV] {len} bytes");
        }
        base.OnMessageReceived(args);
    }

    public async Task SendBytes(int length)
    {
        using (RawMessage newMsg = Parent.CreateMessage())
        {
            var bytes = GenerateBytes(length);
            newMsg.Write(length);
            newMsg.Write(bytes);
            await SendMessageAsync(newMsg, CancellationToken.None);
            _logger.Info($"[{Parent}] [SENT] {length} bytes");
            _sentMessages.Add(bytes);
        }
    }

    public long CrcReceivedMessages()
    {
        var crc32 = new Crc32();
        foreach (var message in _recvMessages)
        {
            crc32.Update(message);
        }

        return crc32.Value;
    }
    
    public long CrcSentMessages()
    {
        var crc32 = new Crc32();
        foreach (var message in _sentMessages)
        {
            crc32.Update(message);
        }

        return crc32.Value;
    }
}