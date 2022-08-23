using System.Buffers;
using Microsoft.IO;
using Neon.Logging;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Tcp;
using Neon.Networking.Tcp.Events;
using Neon.Test.Util;
using Neon.Util.Pooling;

namespace Neon.Test.Tcp
{

    static class Program
    {
        static SingleThreadSynchronizationContext context;
        static ILogger logger;
        
        public static async Task Main(string[] args)
        {
            LogManager logManagerNetworkClient = new LogManager();
            logManagerNetworkClient.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("NET_CLIENT")));
            logManagerNetworkClient.Severity = LogSeverity.TRACE;
            LogManager logManagerNetworkServer = new LogManager();
            logManagerNetworkServer.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("NET_SERVER")));
            logManagerNetworkServer.Severity = LogSeverity.TRACE;
            LogManager logManagerMain = new LogManager();
            logManagerMain.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("MAIN")));
            logManagerMain.Severity = LogSeverity.DEBUG;

            logger = logManagerMain.GetLogger(nameof(Program));

            var streamManager = new RecyclableMemoryStreamManager(1024, 1024, 1024 * 1024, true);
            streamManager.ThrowExceptionOnToArray = true;
            streamManager.GenerateCallStacks = true;
            streamManager.StreamFinalized += StreamManagerOnStreamFinalized;
            streamManager.StreamDoubleDisposed += StreamManagerOnStreamDoubleDisposed;
            MemoryManager memoryManager = new MemoryManager(ArrayPool<byte>.Shared, streamManager);

            context = new SingleThreadSynchronizationContext(logManagerMain);
            context.OnException += ContextOnException;
            context.Start();

            TcpConfigurationServer configurationServer = new TcpConfigurationServer();
            configurationServer.MemoryManager = memoryManager;
            configurationServer.LogManager = logManagerNetworkServer;
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post;
            configurationServer.SetSynchronizationContext(context);
            configurationServer.KeepAliveInterval = 1000;
            configurationServer.KeepAliveTimeout = 10000;
            configurationServer.KeepAliveEnabled = true;
            
            MyTcpServer server = new MyTcpServer(configurationServer);
            server.OnConnectionClosedEvent += ServerOnConnectionClosedEvent;
            server.OnConnectionOpenedEvent += ServerOnConnectionOpenedEvent;
            server.Start();
            server.Listen(10000);

            TcpConfigurationClient configurationClient = new TcpConfigurationClient();
            configurationClient.MemoryManager = memoryManager;
            configurationClient.LogManager = logManagerNetworkClient;
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post;
            configurationClient.SetSynchronizationContext(context);
            configurationClient.KeepAliveInterval = 1000;
            configurationClient.ConnectTimeout = 50;
            // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);

            MyTcpClient client = new MyTcpClient(configurationClient);
            client.OnConnectionClosedEvent += ClientOnConnectionClosedEvent;
            client.OnConnectionOpenedEvent += ClientOnConnectionOpenedEvent;
            client.OnClientStatusChangedEvent += ClientOnStatusChangedEvent;
            client.Start();

            await client.ConnectAsync("127.0.0.1", 10000);
            await Task.Delay(5000);
            client.Disconnect();

            client.Shutdown();
            server.Shutdown();
            
            context.Stop();
            
            logger.Info("DONE!");
        }
        
        static void ContextOnException(Exception ex)
        {
            logger.Critical($"Unhandled exception in context: {ex}");
            Aborter.Abort(127);
        }

        static void StreamManagerOnStreamDoubleDisposed(object? sender, RecyclableMemoryStreamManager.StreamDoubleDisposedEventArgs e)
        {
            throw new InvalidOperationException("Stream was double disposed. alloc:" + e.AllocationStack +
                                                ", dispose1: " + e.DisposeStack1 + ", dispose2: " + e.DisposeStack2);
        }

        static void StreamManagerOnStreamFinalized(object? sender, RecyclableMemoryStreamManager.StreamFinalizedEventArgs e)
        {
            throw new InvalidOperationException($"Stream was finalized {e.Id}: {e.AllocationStack}");
        }

        static void ClientOnStatusChangedEvent(ClientStatusChangedEventArgs args)
        {
            context.CheckThread();
        }

        static void ClientOnConnectionOpenedEvent(ConnectionOpenedEventArgs args)
        {
            context.CheckThread();
        }

        static void ClientOnConnectionClosedEvent(ConnectionClosedEventArgs args)
        {
            context.CheckThread();
        }

        static void ServerOnConnectionOpenedEvent(ConnectionOpenedEventArgs args)
        {
            context.CheckThread();
        }

        static void ServerOnConnectionClosedEvent(ConnectionClosedEventArgs args)
        {
            context.CheckThread();
        }
    }
}