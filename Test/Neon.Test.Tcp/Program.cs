using System.Buffers;
using System.Net;
using System.Text;
using Microsoft.IO;
using Neon.Logging;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Test.Util;
using Neon.Util.Pooling;

namespace Neon.Test.Tcp
{

    static class Program
    {
        static SingleThreadSynchronizationContext? _context;
        static MemoryManager? _memoryManager;
        static ILogger? _logger;

        static LogManager CreateLogManager(string name)
        {
            LogManager logManager = new LogManager();
            logManager.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter(name)));
            return logManager;
        }

        static MyTcpServer CreteTcpServer(bool encrypted)
        {
            //Creating a server configuration
            TcpConfigurationServer configurationServer = new TcpConfigurationServer();
            configurationServer.MemoryManager = _memoryManager; //Setting our memory manager
            configurationServer.LogManager = CreateLogManager("SERVER"); //Setting our log manager
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
            //to reduce network thread sleep time
            configurationServer.SetSynchronizationContext(_context); //Setting out synchronization context
            configurationServer.KeepAliveInterval = 1000; //Settings keep alive
            configurationServer.KeepAliveTimeout = 10000;//Settings keep alive
            configurationServer.KeepAliveEnabled = false;//Settings keep alive
            configurationServer.CompressionThreshold = 1024;
            configurationServer.CompressionLevel = 6;
            if (encrypted)
                configurationServer.CryptographyConfiguration =
                    new CryptographyConfiguration(EncryptionAlgorithmEnum.Aes256, KeyExchangeAlgorithmEnum.Rsa2048);
            
            //Creating server
            MyTcpServer server = new MyTcpServer(configurationServer);
            server.OnConnectionClosedEvent += ServerOnConnectionClosedEvent;
            server.OnConnectionOpenedEvent += ServerOnConnectionOpenedEvent;

            return server;
        }

        static MyTcpClient CreteUnencryptedTcpClient(bool encrypted)
        {
            //Creating client configuration
            TcpConfigurationClient configurationClient = new TcpConfigurationClient();
            configurationClient.MemoryManager = _memoryManager; //Setting our memory manager
            configurationClient.LogManager = CreateLogManager("CLIENT"); //Setting our log manager
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
            //to reduce network thread sleep time
            configurationClient.SetSynchronizationContext(_context); //Setting out synchronization context
            configurationClient.KeepAliveEnabled = false;
            configurationClient.KeepAliveTimeout = 10000;//Settings keep alive
            configurationClient.KeepAliveInterval = 1000;
            configurationClient.ConnectTimeout = 50;
            configurationClient.CompressionThreshold = 1024;
            configurationClient.CompressionLevel = 6;
            if (encrypted)
                configurationClient.CryptographyConfiguration =
                    new CryptographyConfiguration(EncryptionAlgorithmEnum.Aes256, KeyExchangeAlgorithmEnum.Rsa2048);
            // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);

            //Creating client
            MyTcpClient client = new MyTcpClient(configurationClient);
            client.OnConnectionClosedEvent += ClientOnConnectionClosedEvent;
            client.OnConnectionOpenedEvent += ClientOnConnectionOpenedEvent;
            client.OnClientStatusChangedEvent += ClientOnStatusChangedEvent;

            return client;
        }

        public static async Task Main(string[] args)
        {
            //Creating log managers
            LogManager logManagerMain = new LogManager();
            logManagerMain.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("MAIN")));
            
            //Getting the main logger
            _logger = logManagerMain.GetLogger(nameof(Program));
            
            //Creating a new synchronization context for debug purposes
            _context = new SingleThreadSynchronizationContext(logManagerMain);
            _context.OnException += ContextOnException;
            _context.Start();
            
            //Creating custom RecyclableMemoryStreamManager, to catch all undisposed streams
            //Only for debug purposes
            var streamManager = new RecyclableMemoryStreamManager(1024, 1024, 1024 * 1024, true);
            streamManager.ThrowExceptionOnToArray = true;
            streamManager.GenerateCallStacks = true;
            streamManager.StreamFinalized += StreamManagerOnStreamFinalized;
            streamManager.StreamDoubleDisposed += StreamManagerOnStreamDoubleDisposed;
            
            //Creating a new memory manager for debug purposes
            _memoryManager = new MemoryManager(ArrayPool<byte>.Shared, streamManager);

            // await Test(false);
            await Test(true);
            
            _context.Stop();
            _logger.Info("DONE!");
        }

        static async Task Test(bool encrypted)
        {
            var unencryptedTcpServer = CreteTcpServer(encrypted);
            //Starting server
            unencryptedTcpServer.Start();
            //Starting listening on ipv6
            unencryptedTcpServer.Listen(new IPEndPoint(IPAddress.IPv6Loopback, 10000));

            var unencryptedTcpClient = CreteUnencryptedTcpClient(encrypted);
            //Starting the client
            unencryptedTcpClient.Start();

            //Connecting to the server, preferring ipv6 address
            await unencryptedTcpClient.ConnectAsync("localhost", 10000, IPAddressSelectionRules.PreferIpv6, CancellationToken.None);

            var clientConnection = (unencryptedTcpClient.Connection as MyTcpConnection)!;
            var serverConnection = (unencryptedTcpServer.Connections.First().Value as MyTcpConnection)!;

            //Sending a uncompressed chat message
            await clientConnection.SendBytes(byte.MaxValue);
            
            //Sending a compressed chat message
            await clientConnection.SendBytes(ushort.MaxValue);

            using (CancellationTokenSource cts = new CancellationTokenSource(5000))
            {
                while (serverConnection.RecvBytes < ushort.MaxValue + byte.MaxValue)
                {
                    await Task.Delay(500, cts.Token);
                }
            }
            
            if (serverConnection.CrcReceivedMessages() != clientConnection.CrcSentMessages())
                throw new Exception("CRC mismatch");

            //Disconnecting
            unencryptedTcpClient.Disconnect();

            //Shutting down everything
            unencryptedTcpClient.Shutdown();
            unencryptedTcpServer.Shutdown();
        }
        
        static void ContextOnException(Exception ex)
        {
            //If we got exception in context - we failed
            _logger?.Critical($"Unhandled exception in context: {ex}");
            Aborter.Abort(127);
        }

        static void StreamManagerOnStreamDoubleDisposed(object? sender, RecyclableMemoryStreamManager.StreamDoubleDisposedEventArgs e)
        {
            //If stream was double disposed - we failed
            throw new InvalidOperationException("Stream was double disposed. alloc:" + e.AllocationStack +
                                                ", dispose1: " + e.DisposeStack1 + ", dispose2: " + e.DisposeStack2);
        }

        static void StreamManagerOnStreamFinalized(object? sender, RecyclableMemoryStreamManager.StreamFinalizedEventArgs e)
        {
            //If stream was not disposed - we failed
            throw new InvalidOperationException($"Stream was finalized {e.Id}: {e.AllocationStack}");
        }

        static void ClientOnStatusChangedEvent(ClientStatusChangedEventArgs args)
        {
            //Checking right thread
            _context?.CheckThread();
        }

        static void ClientOnConnectionOpenedEvent(ConnectionOpenedEventArgs args)
        {
            //Checking right thread
            _context?.CheckThread();
        }

        static void ClientOnConnectionClosedEvent(ConnectionClosedEventArgs args)
        {
            //Checking right thread
            _context?.CheckThread();
        }

        static void ServerOnConnectionOpenedEvent(ConnectionOpenedEventArgs args)
        {
            //Checking right thread
            _context?.CheckThread();
        }

        static void ServerOnConnectionClosedEvent(ConnectionClosedEventArgs args)
        {
            //Checking right thread
            _context?.CheckThread();
        }
    }
}