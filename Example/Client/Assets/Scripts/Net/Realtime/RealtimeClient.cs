using System.Threading.Tasks;
using Neon.ClientExample.Net.Util;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Udp;
using Neon.Rpc.Serialization;
using Neon.ServerExample.Proto;
using Neon.Util.Pooling;
using UnityEngine;
using ILogger = Neon.Logging.ILogger;

namespace Neon.ClientExample.Net.Realtime
{
    //Unity specific RpcUdpClient wrapper
    //For code simplicity we made it singleton
    public class RealtimeClient : MonoBehaviour
    {
        static ILogger logger = LogManager.Default.GetLogger(nameof(RealtimeClient));
        
        [SerializeField] string serverHost = "localhost";
        [SerializeField] int serverPort = 10000;
        [SerializeField] IPAddressSelectionRules ipAddressSelectionRules;
        
        //current instance, initialized on Awake, just make sure you have only one client
        public static RealtimeClient Instance => instance;
        static RealtimeClient instance;

        //Event invoked on disconnect
        public IGameEvent OnSessionClosed => onSessionClosed;
        //If we connected return the current model of state
        public RealtimeModel Model => ((Session) client.Session)?.Model;

        GameEvent onSessionClosed;
        RpcUdpClient client;

        void Awake()
        {
            //We want to keep it on all scenes
            DontDestroyOnLoad(gameObject);
            
            //Setting the instance 
            instance = this;

            //Setting up logging
            LogManager networkLogManager = new LogManager(LogSeverity.WARNING);
            networkLogManager.Handlers.Add(new UnityLoggingHandler());
            
            LogManager rpcLogManager = new LogManager(LogSeverity.INFO);
            rpcLogManager.Handlers.Add(new UnityLoggingHandler());

            RpcSerializer serializer = new RpcSerializer(MemoryManager.Shared);
            serializer.RegisterTypesFromAssembly(typeof(PlayerProfileProto).Assembly);

            onSessionClosed = new GameEvent();

            //Starting client
            RpcUdpConfigurationClient configurationClient = new RpcUdpConfigurationClient();
            configurationClient.LogManagerNetwork = networkLogManager;
            configurationClient.LogManager = rpcLogManager;
            configurationClient.Serializer = serializer;
            configurationClient.OrderedExecution = true;
            //Very important to disable lambda expressions for Unity, if we're going to use IL2CPP backend  
            configurationClient.RemotingInvocationRules = new RemotingInvocationRules()
                {AllowLambdaExpressions = false, AllowNonPublicMethods = true};
            configurationClient.SetCipher<Aes128Cipher>();
            configurationClient.SessionFactory = new SessionFactory();
            //Capturing current unity context
            configurationClient.CaptureSynchronizationContext();
            //Unity synchronization context keeps correct ordering of posts, so we prefer to use it
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post;

            client = new RpcUdpClient(configurationClient);
            client.OnSessionClosedEvent += ClientOnOnSessionClosedEvent;
            client.Start();
        }

        void OnDestroy()
        {
            //Don't forget to shutdown client on destroy, otherwise Unity may crash
            client.Shutdown();
        }

        public async Task Connect()
        {
            logger.Info($"Connecting to {serverHost}:{serverPort}...");
            await client.OpenConnectionAsync(serverHost, serverPort, ipAddressSelectionRules);
            logger.Info($"Starting new session...");
            await client.StartSessionNoAuth();
            logger.Info($"Session started!");
        }

        //because Udp disconnect is an async operation, we don't need flag like in BackendClient 
        public async void Disconnect()
        {
            await client.CloseAsync();
        }

        void ClientOnOnSessionClosedEvent(SessionClosedEventArgs args)
        {
            logger.Info($"Session closed!");
            onSessionClosed.Invoke(new GameEventInvokationOptions(true, false));
        }
    }
}