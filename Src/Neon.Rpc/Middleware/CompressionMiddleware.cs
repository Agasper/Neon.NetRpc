using System;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using Neon.Logging;
using Neon.Networking.Messages;
using Neon.Rpc.Net.Tcp;

namespace Middleware
{
    public class CompressionMiddleware : IMiddleware
    {
        readonly ILogger logger;
        readonly IMiddlewareConnection connection;
        readonly int compressionThreshold;
        
        public CompressionMiddleware(int compressionThreshold, IMiddlewareConnection connection, ILogManager logManager)
        {
            this.compressionThreshold = compressionThreshold;
            this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
            this.logger = logManager.GetLogger(nameof(CompressionMiddleware));
            this.logger.Meta["connection_id"] = connection.Id;
        }

        public Task Start(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
        
        
        public void MiddlewareMessage(RawMessage message)
        {
            
        }

        public RawMessage ReceiveMessage(RawMessage message)
        {
            if (message == null) throw new ArgumentNullException(nameof(message));
            if (message.Compressed)
            {
                RawMessage uncompressedMessage = null;
                try
                {
                    uncompressedMessage = message.Decompress();
                    logger.Trace($"#{connection.Id} decompressed {message} to {uncompressedMessage}");
                    return uncompressedMessage;
                }
                catch (Exception)
                {
                    uncompressedMessage?.Dispose();
                    throw;
                }
                finally
                {
                    message.Dispose();
                }
            }

            return message;
        }

        public RawMessage SendMessage(RawMessage message)
        {
            if (message.Length >= compressionThreshold)
            {
                RawMessage compressedMessage = null;
                try
                {
                    logger.Trace(
                        $"#{connection.Id} message {message} size exceeds compression threshold {compressionThreshold}, compressing it");

                    compressedMessage = message.Compress(CompressionLevel.Optimal);
                    compressedMessage.Position = 0;

                    logger.Trace($"#{connection.Id} compressed {message} to {compressedMessage}");
                    return compressedMessage;
                }
                catch (Exception)
                {
                    compressedMessage?.Dispose();
                    throw;
                }
                finally
                {
                    message.Dispose();
                }
            }

            return message;
        }
    }
}