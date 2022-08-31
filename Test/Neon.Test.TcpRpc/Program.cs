// See https://aka.ms/new-console-template for more information

#nullable enable
using System;
using System.Buffers;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.IO;
using Neon.Logging;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc.Authorization;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Tcp;
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
            logManagerMain.Severity = LogSeverity.INFO;

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
            RpcTcpConfigurationServer configurationServer = new RpcTcpConfigurationServer();
            configurationServer.MemoryManager = memoryManager; //Setting our memory manager
            configurationServer.SendBufferSize = 4096; //Setting small buffer for debug purposes
            configurationServer.ReceiveBufferSize = 4096; //Setting small buffer for debug purposes
            configurationServer.LogManagerNetwork = logManagerNetworkServer; //Setting our log manager for network
            configurationServer.LogManager = logManagerRpcServer; //Setting our log manager for PRC
            configurationServer.OrderedExecution = true; //Setting the ordered execution of methods
            configurationServer.SetCipher<Aes256Cipher>(); //Adding strong encryption
            configurationServer.SessionFactory = new SessionFactory(context); //Setting our factory
            configurationServer.SetSynchronizationContext(context); //Setting our context
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                            //to reduce network thread sleep time
            configurationServer.CompressionThreshold = 0; //Always compress messages
            configurationServer.AuthSessionFactory = authSessionFactory; //Adding authentication
            configurationServer.Serializer = serializer; //Setting our serializer

            //Creating server
            RpcTcpServer server = new RpcTcpServer(configurationServer);
            server.OnSessionClosedEvent += ServerOnOnSessionClosedEvent;
            server.OnSessionOpenedEvent += ServerOnOnSessionOpenedEvent;
            //Starting server
            server.Start();
            //Starting listening
            server.Listen(10000);

            //Creating client configuration
            RpcTcpConfigurationClient configurationClient = new RpcTcpConfigurationClient();
            configurationClient.MemoryManager = memoryManager; //Setting our memory manager
            configurationClient.SendBufferSize = 4096; //Setting small buffer for debug purposes
            configurationClient.ReceiveBufferSize = 4096; //Setting small buffer for debug purposes
            configurationClient.LogManagerNetwork = logManagerNetworkClient; //Setting our log manager for network
            configurationClient.LogManager = logManagerRpcClient; //Setting our log manager for PRC
            configurationClient.OrderedExecution = true; //Setting the ordered execution of methods
            configurationClient.SetCipher<Aes256Cipher>(); //Adding strong encryption (should be same as in server)
            configurationClient.SessionFactory = new SessionFactory(context); //Setting our factory
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post; //Changing synchronization mode to post,
                                                                                            //to reduce network thread sleep time
            configurationClient.SetSynchronizationContext(context); //Setting our context
            configurationClient.CompressionThreshold = 0; //Always compress messages
                // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);
            configurationClient.Serializer = serializer; //Setting our serializer

            //Creating client
            RpcTcpClient client = new RpcTcpClient(configurationClient);
            client.OnSessionClosedEvent += ClientOnOnSessionClosedEvent;
            client.OnSessionOpenedEvent += ClientOnOnSessionOpenedEvent;
            client.OnStatusChangedEvent += ClientOnOnStatusChangedEvent;
            //Starting the client
            client.Start();
            
            //Testing failed authentication
            await TestFailedAuth(client, authSessionFactory).ConfigureAwait(false);
            //Testing fail without authentication 
            await TestNoAuth(client).ConfigureAwait(false);
            //Testing normal login
            await TestNormalAuth(client, server).ConfigureAwait(false);

            //Closing the connection
            client.Close();
            
            //Shutting down everything
            client.Shutdown();
            server.Shutdown();
            context.Stop();
            
            logger.Info("DONE!");
        }

        static async  Task TestFailedAuth(RpcTcpClient client, TestAuthSessionFactory authSessionFactory)
        {
            //Opening a connection
            await client.OpenConnectionAsync("127.0.0.1", 10000, IPAddressSelectionRules.PreferIpv4);
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
            client.Close();
        }
        
        static async Task TestNoAuth(RpcTcpClient client)
        {
            //Closing previous connection, if not closed
            client.Close();
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
        
        static async Task TestNormalAuth(RpcTcpClient client, RpcTcpServer server)
        {
            //Closing previous connection, if not closed
            client.Close();
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