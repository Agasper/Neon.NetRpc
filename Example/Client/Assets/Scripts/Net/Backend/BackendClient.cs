using System;
using System.Threading.Tasks;
using Google.Protobuf;
using Neon.ClientExample.Net.Util;
using Neon.Logging;
using Neon.Networking;
using Neon.Networking.Cryptography;
using Neon.Rpc;
using Neon.Rpc.Net.Events;
using Neon.Rpc.Net.Tcp;
using Neon.Rpc.Serialization;
using Neon.ServerExample.Proto;
using Neon.Util.Pooling;
using UnityEngine;
using ILogger = Neon.Logging.ILogger;

namespace Neon.ClientExample.Net.Backend
{

    //Unity specific RpcTcpClient wrapper
    //For code simplicity we made it singleton
    public class BackendClient : MonoBehaviour
    {
        static ILogger logger = LogManager.Default.GetLogger(nameof(BackendClient));
        
        [SerializeField] string serverHost = "localhost";
        [SerializeField] int serverPort = 10000;
        [SerializeField] IPAddressSelectionRules ipAddressSelectionRules;
        
        //Current instance, initialized on Awake, just make sure you have only one client
        public static BackendClient Instance => instance;
        static BackendClient instance;

        //Event invoked on disconnect
        public IGameEvent OnSessionClosed => onSessionClosed;
        //If we connected return player profile model, otherwise null
        public PlayerProfileModel ProfileModel => ((Session)client.Session)?.Model;

        GameEvent onSessionClosed;
        RpcTcpClient client;
        bool suppressEvent;

        void Awake()
        {
            //we want to keep it on all scenes
            DontDestroyOnLoad(gameObject);

            //setting the instance 
            instance = this;

            //setting up logging
            LogManager networkLogManager = new LogManager(LogSeverity.WARNING);
            networkLogManager.Handlers.Add(new UnityLoggingHandler());
            
            LogManager rpcLogManager = new LogManager(LogSeverity.INFO);
            rpcLogManager.Handlers.Add(new UnityLoggingHandler());

            RpcSerializer serializer = new RpcSerializer(MemoryManager.Shared);
            serializer.RegisterTypesFromAssembly(typeof(PlayerProfileProto).Assembly);

            onSessionClosed = new GameEvent();

            //starting client
            RpcTcpConfigurationClient configurationClient = new RpcTcpConfigurationClient();
            configurationClient.LogManagerNetwork = networkLogManager;
            configurationClient.LogManager = rpcLogManager;
            configurationClient.Serializer = serializer;
            configurationClient.OrderedExecution = true;
            //very important to disable lambda expressions for Unity, if we're going to use IL2CPP backend  
            configurationClient.RemotingInvocationRules = new RemotingInvocationRules()
                {AllowLambdaExpressions = false, AllowNonPublicMethods = true};
            configurationClient.SetCipher<Aes128Cipher>();
            configurationClient.SessionFactory = new SessionFactory();
            //capturing current unity context
            configurationClient.CaptureSynchronizationContext();
            //Unity synchronization context keeps correct ordering of posts, so we prefer to use it
            configurationClient.ContextSynchronizationMode = ContextSynchronizationMode.Post;

            client = new RpcTcpClient(configurationClient);
            client.OnSessionClosedEvent += ClientOnOnSessionClosedEvent;
            client.Start();
        }

        void OnDestroy()
        {
            //don't forget to shutdown client on destroy, otherwise Unity may crash
            client.Shutdown();
        }


        public async Task Connect()
        {
            try
            {
                logger.Info($"Connecting to {serverHost}:{serverPort}...");
                await client.OpenConnectionAsync(serverHost, serverPort, ipAddressSelectionRules);
                logger.Info($"Starting new session...");
                await client.StartSessionWithAuth(CredentialsStorage.GetCredentials());
                Session session = (Session) client.Session;
                if (session.NewCredentials != null) //if it's set, we have a new credentials
                    CredentialsStorage.SetCredentials(session.NewCredentials);
                logger.Info($"Getting profile...");
                await session.CreateModel();
                logger.Info($"Session opened!");
            }
            catch
            {
                //in case of exception we close connection
                client.Close();
                throw;
            }
        }

        public void Disconnect()
        {
            //if we disconnects from the server by our will, we don't
            //want to raise event, and popup "connection terminated" message
            if (client.Status == RpcClientStatus.SessionReady)
                suppressEvent = true;
            client.Close();
        }

        void ClientOnOnSessionClosedEvent(SessionClosedEventArgs args)
        {
            logger.Info($"Session closed!");
            //if we disconnects from the server by our will, we don't
            //want to raise event, and popup "connection terminated" message
            if (!suppressEvent)
                onSessionClosed.Invoke(new GameEventInvokationOptions(true, false));
            //resetting the flag
            suppressEvent = false;
        }
    }
}