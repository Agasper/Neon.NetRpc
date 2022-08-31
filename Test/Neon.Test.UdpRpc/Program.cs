using System.Buffers;
using Microsoft.IO;
using Neon.Logging;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Udp;
using Neon.Rpc.Payload;
using Neon.Rpc.Serialization;
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
            LogManager logManagerNetworkClient = new LogManager();
            logManagerNetworkClient.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("NET_CLIENT")));
            logManagerNetworkClient.Severity = LogSeverity.TRACE;
            LogManager logManagerNetworkServer = new LogManager();
            logManagerNetworkServer.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("NET_SERVER")));
            logManagerNetworkServer.Severity = LogSeverity.TRACE;
            LogManager logManagerRpcClient = new LogManager();
            logManagerRpcClient.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("RPC_CLIENT")));
            logManagerRpcClient.Severity = LogSeverity.TRACE;
            LogManager logManagerRpcServer = new LogManager();
            logManagerRpcServer.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("RPC_SERVER")));
            logManagerRpcServer.Severity = LogSeverity.TRACE;
            LogManager logManagerMain = new LogManager();
            logManagerMain.Handlers.Add(new LoggingHandlerConsole(new NamedLoggingFormatter("MAIN")));
            logManagerMain.Severity = LogSeverity.DEBUG;

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

            //Creating out authentication factory
            TestAuthSessionFactory authSessionFactory = new TestAuthSessionFactory();
            
            //Creating a new serializer
            RpcSerializer serializer = new RpcSerializer(memoryManager);
            //Registering our messages
            serializer.RegisterTypesFromAssembly(typeof(TestMessage).Assembly);

            //Creating a server configuration
            RpcUdpConfigurationServer configurationServer = new RpcUdpConfigurationServer();
            configurationServer.MemoryManager = memoryManager; //Setting our memory manager
            configurationServer.SendBufferSize = 4096; //Setting small buffer for debug purposes
            configurationServer.ReceiveBufferSize = 4096; //Setting small buffer for debug purposes
            configurationServer.NetworkLogManager = logManagerNetworkServer;  //Setting our log manager for network
            configurationServer.LogManager = logManagerRpcServer; //Setting our log manager for PRC
            configurationServer.OrderedExecution = true; //Setting the ordered execution of methods
            configurationServer.SetCipher<Aes128Cipher>(); //Adding strong encryption
            configurationServer.SessionFactory = new SessionFactory(context); //Setting our factory
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                    //to reduce network thread sleep time
            configurationServer.SetSynchronizationContext(context); //Setting our context
            configurationServer.CompressionThreshold = 0; //Always compress messages
            configurationServer.AuthSessionFactory = authSessionFactory; //Adding authentication
            configurationServer.Serializer = serializer; //Setting our serializer

            //Creating server
            RpcUdpServer server = new RpcUdpServer(configurationServer);
            server.OnSessionClosedEvent += ServerOnOnSessionClosedEvent;
            server.OnSessionOpenedEvent += ServerOnOnSessionOpenedEvent;
            //Starting server
            server.Start();
            //Starting listening
            server.Listen(10000);

            //Creating client configuration
            RpcUdpConfigurationClient configurationClient = new RpcUdpConfigurationClient();
            configurationClient.MemoryManager = memoryManager; //Setting our memory manager
            configurationClient.SendBufferSize = 4096; //Setting small buffer for debug purposes
            configurationClient.ReceiveBufferSize = 4096; //Setting small buffer for debug purposes
            configurationClient.NetworkLogManager = logManagerNetworkClient; //Setting our log manager for network
            configurationClient.LogManager = logManagerRpcClient; //Setting our log manager for PRC
            configurationClient.OrderedExecution = true; //Setting the ordered execution of methods
            configurationClient.SetCipher<Aes128Cipher>(); //Adding strong encryption (should be same as in server)
            configurationClient.SessionFactory = new SessionFactory(context); //Setting our factory
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                         //to reduce network thread sleep time
            configurationClient.SetSynchronizationContext(context); //Setting our context
            // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);
            configurationClient.Serializer = serializer; //Setting our serializer

            //Creating client
            RpcUdpClient client = new RpcUdpClient(configurationClient);
            client.OnSessionClosedEvent += ClientOnOnSessionClosedEvent;
            client.OnSessionOpenedEvent += ClientOnOnSessionOpenedEvent;
            client.OnStatusChangedEvent += ClientOnOnStatusChangedEvent;
            //Starting client
            client.Start();

            //Testing failed authentication
            await TestFailedAuth(client, authSessionFactory).ConfigureAwait(false);
            //Testing fail without authentication 
            await TestNoAuth(client).ConfigureAwait(false);
            //Testing normal login
            await TestNormalAuth(client, server).ConfigureAwait(false);
            
            //Shutting down everything
            client.Shutdown();
            server.Shutdown();
            
            context.Stop();
            
            logger.Info("DONE!");
        }
        
        static async  Task TestFailedAuth(RpcUdpClient client, TestAuthSessionFactory authSessionFactory)
        {
            //Opening a connection
            await client.OpenConnectionAsync("127.0.0.1", 10000);
            try
            {
                //Testing non-async auth session
                authSessionFactory.ReturnAsync = false;
                //Starting the session with wrong credentials
                await client.StartSessionWithAuth(new AuthTest() {Login = "123", Password = "1234"});
                throw new InvalidOperationException($"Test {nameof(TestFailedAuth)} failed");
            }
            catch (RemotingException e)
            {
                if (e.StatusCode != RemotingException.StatusCodeEnum.AccessDenied)
                    throw new InvalidOperationException("Auth test failed");
            }

            try
            {
                //Testing async auth session
                authSessionFactory.ReturnAsync = true;
                //Starting the session with wrong credentials
                await client.StartSessionWithAuth(new AuthTest() {Login = "123", Password = "12345"});
                throw new InvalidOperationException($"Test {nameof(TestFailedAuth)} failed");
            }
            catch (RemotingException e)
            {
                if (e.StatusCode != RemotingException.StatusCodeEnum.AccessDenied)
                    throw new InvalidOperationException("Auth test failed");
            }
            
            //Closing connection
            await client.CloseAsync();
        }
        
        static async Task TestNoAuth(RpcUdpClient client)
        {
            //Closing previous connection, if not closed
            await client.CloseAsync();
            //Opening a connection
            await client.OpenConnectionAsync("127.0.0.1", 10000);
            //Starting a new session without authentication
            await client.StartSessionNoAuth();

            try
            {
                //Trying to call methods
                await new BasicTest(client.Session).Run().ConfigureAwait(false);
                //If method executed we failed
                throw new InvalidOperationException($"Test {nameof(TestNoAuth)} failed");
            }
            catch (RemotingException e)
            {
                if (e.StatusCode != RemotingException.StatusCodeEnum.AccessDenied)
                    throw new InvalidOperationException("Auth test failed");
            }
        }
        
        static async Task TestNormalAuth(RpcUdpClient client, RpcUdpServer server)
        {
            //Closing previous connection, if not closed
            await client.CloseAsync();
            //Opening a connection
            await client.OpenConnectionAsync("127.0.0.1", 10000);
            //Starting a new session with correct credentials
            await client.StartSessionWithAuth(new AuthTest() {Login = "123", Password = "123"});
            
            //Testing methods from the client
            BasicTest basicTestClient = new BasicTest(client.Session);
            await basicTestClient.Run().ConfigureAwait(false);
            
            //Testing method from the server
            BasicTest basicTestServer = new BasicTest(server.Sessions.First());
            await basicTestServer.Run().ConfigureAwait(false);

            //Testing big messages
            BufferTest bufferTest = new BufferTest(client.Session, 100000);
            await bufferTest.Run().ConfigureAwait(false);
            
            //Testing async methods
            TaskTest taskTest = new TaskTest(client.Session);
            await taskTest.Run().ConfigureAwait(false);

            //Closing connection
            await client.CloseAsync().ConfigureAwait(false);
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

        static void ClientOnOnStatusChangedEvent(RpcClientStatusChangedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ClientOnOnSessionOpenedEvent(SessionOpenedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ClientOnOnSessionClosedEvent(SessionClosedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ServerOnOnSessionOpenedEvent(SessionOpenedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }

        static void ServerOnOnSessionClosedEvent(SessionClosedEventArgs args)
        {
            //Checking right thread
            context?.CheckThread();
        }
    }
}