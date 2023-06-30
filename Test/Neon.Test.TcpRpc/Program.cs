// See https://aka.ms/new-console-template for more information

#nullable enable
using System;
using System.Buffers;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.IO;
using Neon.Logging;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc;
using Neon.Rpc.Cryptography;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Tcp;
using Neon.Rpc.Net.Tcp.Events;
using Neon.Test.Proto;
using Neon.Test.Util;
using Neon.Util.Pooling;

namespace Neon.Test.TcpRpc
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
            logManagerServer.LoggingNameFilters.Add(new LoggingNameFilter("Neon.Networking.Tcp.TcpConnection", LogSeverity.DEBUG));
            LogManager logManagerClient = new LogManager();
            logManagerClient.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("CLIENT")));
            logManagerClient.LoggingNameFilters.Add(new LoggingNameFilter("Neon.Networking.Tcp.TcpConnection", LogSeverity.DEBUG));

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
            RpcTcpConfigurationServer configurationServer = new RpcTcpConfigurationServer();
            configurationServer.MemoryManager = memoryManager; //Setting our memory manager
            configurationServer.SendBufferSize = 4096; //Setting small buffer for debug purposes
            configurationServer.ReceiveBufferSize = 4096; //Setting small buffer for debug purposes
            configurationServer.LogManager = logManagerServer; //Setting our log manager for PRC
            configurationServer.OrderedExecution = true; //Setting the ordered execution of methods
            configurationServer.SessionFactory = new SessionFactory(context); //Setting our factory
            configurationServer.SetSynchronizationContext(context); //Setting our context
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                            //to reduce network thread sleep time
            // configurationServer.CompressionThreshold = 0; //Always compress messages

            //Creating server
            RpcTcpServer server = new RpcTcpServer(configurationServer);
            //Starting server
            server.Start();
            //Starting listening
            server.Listen(10000);

            //Creating client configuration
            RpcTcpConfigurationClient configurationClient = new RpcTcpConfigurationClient();
            configurationClient.MemoryManager = memoryManager; //Setting our memory manager
            configurationClient.SendBufferSize = 4096; //Setting small buffer for debug purposes
            configurationClient.ReceiveBufferSize = 4096; //Setting small buffer for debug purposes
            configurationClient.LogManager = logManagerClient; //Setting our log manager for PRC
            configurationClient.OrderedExecution = true; //Setting the ordered execution of methods
            configurationClient.SessionFactory = new SessionFactory(context); //Setting our factory
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                            //to reduce network thread sleep time
            configurationClient.SetSynchronizationContext(context); //Setting our context
            // configurationClient.CompressionThreshold = 0; //Always compress messages
                // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);

                //Creating client
            RpcTcpClient client = new RpcTcpClient(configurationClient);
            client.OnStatusChangedEvent += ClientOnOnStatusChangedEvent;
            //Starting the client
            client.Start();
            
            //Testing normal login
            await TestNormalAuth(client, server, new AuthenticationInfo(), CancellationToken.None).ConfigureAwait(false);

            //Closing the connection
            client.Close();
            
            //Shutting down everything
            client.Shutdown();
            server.Shutdown();
            context.Stop();
            
            logger.Info("DONE!");
        }

        static void MemoryManagerOnOnObjectFinalized(object sender, ObjectFinalizedEventArgs e)
        {
            logger!.Critical($"Object finalized ({sender}): {e.ObjectType} #{e.Id} -> {e.AllocationStack}");
        }

        static async Task TestNormalAuth(RpcTcpClient client, RpcTcpServer server, AuthenticationInfo authenticationInfo, CancellationToken cancellationToken)
        {
            await client.StartSessionAsync("127.0.0.1", 10000, IPAddressSelectionRules.OnlyIpv4, authenticationInfo, cancellationToken);
            
            //Testing methods from the client
            BasicTest basicTestClient = new BasicTest(client.Session);
            await basicTestClient.Run().ConfigureAwait(false);
            
            //Testing method from the server
            BasicTest basicTestServer = new BasicTest(server.Sessions.First());
            await basicTestServer.Run().ConfigureAwait(false);

            //Testing big messages
            BufferTest bufferTest = new BufferTest(client.Session, 100000);
            await bufferTest.Run().ConfigureAwait(false);
            
            //Closing connection
            client.Close();
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

        static void ClientOnOnStatusChangedEvent(RpcTcpClientStatusChangedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }
        //
        // static void ClientOnOnSessionOpenedEvent(SessionOpenedEventArgs args)
        // {
        //     //Checking right thread
        //     context?.CheckThread();
        // }
        //
        // static void ClientOnOnSessionClosedEvent(SessionClosedEventArgs args)
        // {
        //     //Checking right thread
        //     context?.CheckThread();
        // }
        //
        // static void ServerOnOnSessionOpenedEvent(SessionOpenedEventArgs args)
        // {
        //     //Checking right thread
        //     context?.CheckThread();
        // }
        //
        // static void ServerOnOnSessionClosedEvent(SessionClosedEventArgs args)
        // {
        //     //Checking right thread
        //     context?.CheckThread();
        // }
    }
}