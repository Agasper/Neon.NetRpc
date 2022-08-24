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
            
            TestAuthSessionFactory authSessionFactory = new TestAuthSessionFactory();

            RpcTcpConfigurationServer configurationServer = new RpcTcpConfigurationServer();
            configurationServer.MemoryManager = memoryManager;
            configurationServer.SendBufferSize = 4096;
            configurationServer.ReceiveBufferSize = 4096;
            configurationServer.LogManagerNetwork = logManagerNetworkServer;
            configurationServer.LogManager = logManagerRpcServer;
            configurationServer.OrderedExecution = true;
            configurationServer.SetCipher<Aes256Cipher>();
            configurationServer.SessionFactory = new SessionFactory(context);
            configurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Post;
            configurationServer.SetSynchronizationContext(context);
            configurationServer.KeepAliveEnabled = true;
            configurationServer.KeepAliveInterval = 1000;
            configurationServer.KeepAliveTimeout = 10000;
            configurationServer.CompressionThreshold = 0;
            configurationServer.AuthSessionFactory = authSessionFactory;
            configurationServer.Serializer = new RpcSerializer(memoryManager);
            configurationServer.Serializer.RegisterTypesFromAssembly(typeof(TestMessage).Assembly);
            
            RpcTcpServer server = new RpcTcpServer(configurationServer);
            server.OnSessionClosedEvent += ServerOnOnSessionClosedEvent;
            server.OnSessionOpenedEvent += ServerOnOnSessionOpenedEvent;
            server.Start();
            server.Listen(10000);

            RpcTcpConfigurationClient configurationClient = new RpcTcpConfigurationClient();
            configurationClient.MemoryManager = memoryManager;
            configurationClient.SendBufferSize = 4096;
            configurationClient.ReceiveBufferSize = 4096;
            configurationClient.LogManagerNetwork = logManagerNetworkClient;
            configurationClient.LogManager = logManagerRpcClient;
            configurationClient.OrderedExecution = true;
            configurationClient.ConnectTimeout = 5000;
            configurationClient.SetCipher<Aes256Cipher>();
            configurationClient.SessionFactory = new SessionFactory(context);
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post;
            configurationClient.SetSynchronizationContext(context);
            configurationClient.KeepAliveEnabled = true;
            configurationClient.KeepAliveInterval = 1000;
            configurationClient.KeepAliveTimeout = 10000;
            configurationClient.CompressionThreshold = 0;
                // configurationClient.ConnectionSimulation = new ConnectionSimulation(2000, 1000);
            configurationClient.Serializer = new RpcSerializer(memoryManager);
            configurationClient.Serializer.RegisterTypesFromAssembly(typeof(TestMessage).Assembly);

            RpcTcpClient client = new RpcTcpClient(configurationClient);
            client.OnSessionClosedEvent += ClientOnOnSessionClosedEvent;
            client.OnSessionOpenedEvent += ClientOnOnSessionOpenedEvent;
            client.OnStatusChangedEvent += ClientOnOnStatusChangedEvent;
            client.Start();
            
            await TestFailedAuth(client, authSessionFactory).ConfigureAwait(false);
            await TestNoAuth(client).ConfigureAwait(false);
            await TestNormalAuth(client, server).ConfigureAwait(false);

            client.Close();

            client.Shutdown();
            server.Shutdown();
            
            context.Stop();
            
            logger.Info("DONE!");
        }

        static async  Task TestFailedAuth(RpcTcpClient client, TestAuthSessionFactory authSessionFactory)
        {
            await client.OpenConnectionAsync("127.0.0.1", 10000);
            try
            {
                authSessionFactory.ReturnAsync = false;
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
                authSessionFactory.ReturnAsync = true;
                await client.StartSessionWithAuth(new AuthTest() {Login = "123", Password = "12345"});
                throw new InvalidOperationException($"Test {nameof(TestFailedAuth)} failed");
            }
            catch (RemotingException e)
            {
                if (e.StatusCode != RemotingException.StatusCodeEnum.AccessDenied)
                    throw new InvalidOperationException("Auth test failed");
            }
            
            client.Close();
        }
        
        static async Task TestNoAuth(RpcTcpClient client)
        {
            client.Close();
            await client.OpenConnectionAsync("127.0.0.1", 10000);
            await client.StartSessionNoAuth();

            try
            {
                await new BasicTest(client.Session).Run().ConfigureAwait(false);
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
            client.Close();
            await client.OpenConnectionAsync("127.0.0.1", 10000);
            await client.StartSessionWithAuth(new AuthTest() {Login = "123", Password = "123"});
            
            BasicTest basicTestClient = new BasicTest(client.Session);
            await basicTestClient.Run().ConfigureAwait(false);
            
            BasicTest basicTestServer = new BasicTest(server.Sessions.First());
            await basicTestServer.Run().ConfigureAwait(false);

            BufferTest bufferTest = new BufferTest(client.Session, 100000);
            
            await bufferTest.Run().ConfigureAwait(false);

            client.Close();
        }

        static void ContextOnException(Exception ex)
        {
            logger?.Critical($"Unhandled exception in context: {ex}");
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

        static void ClientOnOnStatusChangedEvent(RpcClientStatusChangedEventArgs args)
        {
            context?.CheckThread();
        }

        static void ClientOnOnSessionOpenedEvent(SessionOpenedEventArgs args)
        {
            context?.CheckThread();
        }

        static void ClientOnOnSessionClosedEvent(SessionClosedEventArgs args)
        {
            context?.CheckThread();
        }

        static void ServerOnOnSessionOpenedEvent(SessionOpenedEventArgs args)
        {
            context?.CheckThread();
        }

        static void ServerOnOnSessionClosedEvent(SessionClosedEventArgs args)
        {
            context?.CheckThread();
        }
    }
}