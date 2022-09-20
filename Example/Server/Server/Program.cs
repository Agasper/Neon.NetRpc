using System.Diagnostics;
using System.Reflection;
using NDesk.Options;
using Neon.Logging;
using Neon.Logging.Formatters;
using Neon.Logging.Handlers;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc.Net.Tcp;
using Neon.Rpc.Net.Udp;
using Neon.Rpc.Serialization;
using Neon.ServerExample.Backend;
using Neon.ServerExample.Proto;
using Neon.ServerExample.Realtime.Rooms;
using Neon.ServerExample.Util;
using Neon.Util.Pooling;
using Niarru.Zodchy.Server.Data;

namespace Neon.ServerExample
{
    static class Program
    {
        const int LISTEN_PORT_DEFAULT = 10000;

        static IDataStore<PlayerProfileProto>? dataStore;
        static RoomController? roomController;
        static ILogger? logger;
        static RpcTcpServer? backendServer;
        static RpcUdpServer? realtimeServer;
        static SignalHelper? signaler;

        public static async Task Main(string[] args)
        {
            //We want to catch all unhandled exceptions before app crashed
            AppDomain.CurrentDomain.UnhandledException += CurrentDomainOnUnhandledException;
            
            Assembly assembly = Assembly.GetEntryAssembly() ?? throw new InvalidOperationException("Entry assembly is null");
            //getting app path
            string appPath = Path.GetFullPath(Path.GetDirectoryName(assembly.Location) ??
                                              throw new InvalidOperationException(
                                                  "Couldn't get assembly root directory"));
            
            //default startup parameters sets here
            LogSeverity logsSeverity = LogSeverity.INFO;
            LogSeverity logsNetworkSeverity = LogSeverity.WARNING;
            LogSeverity logsRpcSeverity = LogSeverity.INFO;
            DataStoreType dataStoreType = DataStoreType.SqLite;
            string dbHost = Path.Combine(appPath, "datastore.bin");
            bool show_help = false;
            string? listenHost =null;
            int listenPort = LISTEN_PORT_DEFAULT;
            
            //parsing arguments to override parameters
            var p = new OptionSet()
            {
                {
                    "log_severity=", "Logs severity - TRACE, DEBUG, INFO (default), WARNING, ERROR, CRITICAL",
                    v => logsSeverity = (LogSeverity) Enum.Parse(typeof(LogSeverity), v)
                },
                {
                    "log_network_severity=",
                    "Network logs severity - TRACE, DEBUG, INFO, WARNING (default), ERROR, CRITICAL",
                    v => logsNetworkSeverity = (LogSeverity) Enum.Parse(typeof(LogSeverity), v)
                },
                {
                    "log_rpc_severity=", "RPC logs severity - TRACE, DEBUG, INFO, WARNING (default), ERROR, CRITICAL",
                    v => logsRpcSeverity = (LogSeverity) Enum.Parse(typeof(LogSeverity), v)
                },
                {
                    "db_type=", $"DB Type: {DataStoreType.SqLite}",
                    v => dataStoreType = (DataStoreType) Enum.Parse(typeof(DataStoreType), v)
                },
                {
                    "db_host=", $"DB Host",
                    v => dbHost = v
                },
                { "host=", "Listen host", v => listenHost = v},
                {
                    "port|p=", $"Listen port (default {LISTEN_PORT_DEFAULT})",
                    v => listenPort = int.Parse(v)
                },
                {
                    "h|help", "show this message and exit",
                    v => show_help = v != null
                },
            };

            try
            {
                p.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine("Wrong arguments: " + e.Message);
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            if (show_help)
            {
                p.WriteOptionDescriptions(Console.Out);
                return;
            }

            //setting up logging
            //we're ok with default handler and default formatter
            var handler =
                new LoggingHandlerConsole(new LoggingFormatterDefault());
            //setting the default log manager
            LogManager.SetDefault(new LogManager(logsSeverity, handler));
            //log manager for network logs
            LogManager networkLogManager = new LogManager(logsNetworkSeverity, handler);
            //log manager for rpc calls
            LogManager rpcLogManager = new LogManager(logsRpcSeverity, handler);
            
            logger = LogManager.Default.GetLogger(nameof(Program));

            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.ProductVersion ?? "unknown";

            logger.Info($"Starting Neon Server v{version}...");
            
            logger.Info("Checking database connection...");
            //checking database type, this example supports only SQLite
            if (dataStoreType == DataStoreType.SqLite)
            {
                //creating SQLite datastore
                dataStore = new DataStoreSqlite<PlayerProfileProto>(MemoryManager.Shared, dbHost);
            }
            else
            {
                throw new NotImplementedException($"Datastore types other than {DataStoreType.SqLite} are not supported");
            }

            //Checking db exists and ready
            await dataStore.DbCheck();

            //Starting battle rooms controller
            roomController = new RoomController();
            roomController.Start();

            //Creating TCP & UDP servers
            
            RpcSerializer serializer = new RpcSerializer(MemoryManager.Shared);
            serializer.RegisterTypesFromAssembly(typeof(PlayerProfileProto).Assembly);
            
            logger.Info("Creating backend server...");
            RpcTcpConfigurationServer rpcTcpConfigurationServer = new RpcTcpConfigurationServer();
            rpcTcpConfigurationServer.LogManager = rpcLogManager;
            rpcTcpConfigurationServer.LogManagerNetwork = networkLogManager;
            rpcTcpConfigurationServer.OrderedExecution = true;
            rpcTcpConfigurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Send;
            rpcTcpConfigurationServer.SessionFactory = new PlayerSessionFactory(dataStore, roomController);
            rpcTcpConfigurationServer.AuthSessionFactory = new AuthSessionFactory(dataStore);
            rpcTcpConfigurationServer.Serializer = serializer;
            rpcTcpConfigurationServer.SetCipher<Aes128Cipher>();

            RpcUdpConfigurationServer rpcUdpConfigurationServer = new RpcUdpConfigurationServer();
            rpcUdpConfigurationServer.LogManager = rpcLogManager;
            rpcUdpConfigurationServer.LogManagerNetwork = networkLogManager;
            rpcUdpConfigurationServer.Serializer = serializer;
            rpcUdpConfigurationServer.OrderedExecution = true;
            rpcUdpConfigurationServer.ContextSynchronizationMode = ContextSynchronizationMode.Send;
            rpcUdpConfigurationServer.SetCipher<Aes128Cipher>();
            rpcUdpConfigurationServer.SessionFactory = new Realtime.PlayerSessionFactory(roomController);

            backendServer = new RpcTcpServer(rpcTcpConfigurationServer);
            backendServer.Start();
            backendServer.Listen(listenHost, listenPort);

            realtimeServer = new RpcUdpServer(rpcUdpConfigurationServer);
            realtimeServer.Start();
            realtimeServer.Listen(listenHost, listenPort);
            
            logger.Info("Server started!");
            
            //Waiting for signal to stop the app
            signaler = new SignalHelper();
            signaler.Wait();
            
            logger.Info("Stopping server...");
            Stop();
        }

        static void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            if (logger != null)
                logger.Critical($"Unhandled exception ({sender}): {e.ExceptionObject}");
            else
                Console.WriteLine($"Unhandled exception ({sender}): {e.ExceptionObject}");
        }
        
        static void Stop()
        {
            //stopping everything
            
            if (realtimeServer != null)
                realtimeServer.Shutdown();
            
            if (backendServer != null)
                backendServer.Shutdown();

            if (roomController != null)
                roomController.Stop();
            
            if (signaler != null)
                signaler.Dispose();
        }
    }
}