using System.Buffers;
using System.Net;
using Microsoft.IO;
using Neon.Logging;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Udp;
using Neon.Networking.Udp.Events;
using Neon.Test.Util;
using Neon.Util.Pooling;

namespace Neon.Test.Udp
{

    static class Program
    {
        static SingleThreadSynchronizationContext? context;
        static ILogger? logger;
        
        public static async Task Main(string[] args)
        {
            //Creating log managers
            LogManager logManagerMain = new LogManager();
            logManagerMain.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("MAIN")));
            LogManager logManagerServer = new LogManager();
            logManagerServer.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("SERVER")));
            LogManager logManagerClient = new LogManager();
            logManagerClient.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("CLIENT")));

            //Getting the main logger
            logger = logManagerMain.GetLogger(nameof(Program));

            //Creating custom RecyclableMemoryStreamManager, to catch all undisposed streams
            //Only for debug purposes
            var streamManager = new RecyclableMemoryStreamManager(1024, 1024, 1024 * 1024, true);
            streamManager.ThrowExceptionOnToArray = true;
            streamManager.GenerateCallStacks = true;
            streamManager.StreamFinalized += StreamManagerOnStreamFinalized;
            streamManager.StreamDoubleDisposed += StreamManagerOnStreamDoubleDisposed;
            
            //Creating a new memory manager for debug purposes
            MemoryManager memoryManager = new MemoryManager(ArrayPool<byte>.Shared, streamManager);

            //Creating a new synchronization context for debug purposes
            context = new SingleThreadSynchronizationContext(logManagerMain);
            context.OnException += ContextOnException;
            context.Start();

            //Creating a server configuration
            UdpConfigurationServer configurationServer = new UdpConfigurationServer();
            configurationServer.MemoryManager = memoryManager; //Setting our memory manager
            configurationServer.LogManager = logManagerServer; //Setting our log manager
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                        //to reduce network thread sleep time
            configurationServer.SetSynchronizationContext(context); //Setting out synchronization context

            //Creating server
            MyUdpServer server = new MyUdpServer(configurationServer);
            server.OnConnectionClosedEvent += ServerOnConnectionClosedEvent;
            server.OnConnectionOpenedEvent += ServerOnConnectionOpenedEvent;
            //Starting server
            server.Start();
            //Starting listening on ipv6
            server.Listen(new IPEndPoint(IPAddress.IPv6Loopback, 10000));

            //Creating client configuration
            UdpConfigurationClient configurationClient = new UdpConfigurationClient();
            configurationClient.MemoryManager = memoryManager; //Setting our memory manager
            configurationClient.LogManager = logManagerClient; //Setting our log manager
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post;  //Changing synchronization mode to post,
                                                                                            //to reduce network thread sleep time
            configurationClient.SetSynchronizationContext(context); //Setting out synchronization context
            // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);

            //Creating client
            MyUdpClient client = new MyUdpClient(configurationClient);
            client.OnConnectionClosedEvent += ClientOnConnectionClosedEvent;
            client.OnConnectionOpenedEvent += ClientOnConnectionOpenedEvent;
            client.OnClientStatusChangedEvent += ClientOnStatusChangedEvent;
            //Starting the client
            client.Start();

            //Connecting to the server, preferring ipv6 address
            await client.ConnectAsync("localhost", 10000, IPAddressSelectionRules.PreferIpv6, CancellationToken.None);
            //Disconnecting
            await client.DisconnectAsync();

            //Shutting down everything
            client.Shutdown();
            server.Shutdown();
            
            context.Stop();
            
            logger.Info("DONE!");
        }
        
        static void ContextOnException(Exception ex)
        {
            //If we got exception in context - we failed
            logger?.Critical($"Unhandled exception in context: {ex}");
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
            context?.CheckThread();
        }

        static void ClientOnConnectionOpenedEvent(ConnectionOpenedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ClientOnConnectionClosedEvent(ConnectionClosedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ServerOnConnectionOpenedEvent(ConnectionOpenedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ServerOnConnectionClosedEvent(ConnectionClosedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }
    }
}