using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Neon.Logging;
using Neon.Networking.Cryptography;
using Neon.Rpc.Controllers.States;
using Neon.Rpc.Cryptography;
using Neon.Rpc.Cryptography.Ciphers;
using Neon.Rpc.Cryptography.KeyExchange;
using Neon.Rpc.Messages;
using Neon.Rpc.Messages.Proto;

namespace Neon.Rpc.Controllers
{
    class RpcController : RpcControllerBase
    {
        public RpcSessionBase UserSession => _userState._session;
        
        const string HANDSHAKE_METHOD = "@HANDSHAKE";
        const string SET_ENCRYPTION_METHOD = "@SET_ENCRYPTION";
        const string AUTH_METHOD = "@AUTH";
        const string USER_METHOD_PREFIX = "#";

        HandshakeState _handshakeState;
        AuthState _authState;
        UserState _userState;

        readonly SemaphoreSlim _systemSemaphore;
        new readonly ILogger _logger;

        public RpcController(RpcControllerContext controllerContext) : base(controllerContext)
        {
            _authState._started = DateTimeOffset.UtcNow;
            _systemSemaphore = new SemaphoreSlim(1, 1);
            _logger = controllerContext.Configuration.LogManager.GetLogger(typeof(RpcController));
        }
        
        

        private protected override void OnDisposed()
        {
            base.OnDisposed();
            
            _systemSemaphore?.Dispose();
            _handshakeState.NegotiatedRpcCipher?.Dispose();

            if (_userState._session != null)
                _ = _userState._session?.CloseGracefully(CancellationToken.None);
        }


        public async Task Handshake(CancellationToken cancellationToken)
        {
            CheckDisposed();
            using (var combinedCts =
                   CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                       _context.Connection.CancellationToken))
            {
                await _systemSemaphore.WaitAsync(combinedCts.Token).ConfigureAwait(false);
                try
                {
                    _handshakeState.CheckCanSend(_context.Connection.IsClientConnection);

                    RpcHandshakeRequestProto handshakeRequest = new RpcHandshakeRequestProto();
                    handshakeRequest.ProtocolVersion = Constants.ProtocolVersion;

                    combinedCts.Token.ThrowIfCancellationRequested();
                    if (_context.Configuration.CryptographyConfiguration.EncryptionAlgorithm == EncryptionAlgorithmEnum.None)
                    {
                        RpcHandshakeResponseProto handshakeResponse =
                            await ExecuteInternalAsync<RpcHandshakeResponseProto>(HANDSHAKE_METHOD,
                                    handshakeRequest,
                                    ExecutionOptions.Default.WithCancellationToken(combinedCts.Token))
                                .ConfigureAwait(false);
                        _handshakeState._handshakeCompleted = true;
                        return;
                    }

                    IRpcCipher rpcCipher = null;
                    try
                    {
                        rpcCipher = CryptographyHelper.CreateCipher(_context.Configuration.CryptographyConfiguration
                            .EncryptionAlgorithm);
                        IKeyExchangeAlgorithm keyExchangeAlgorithm =
                            CryptographyHelper.CreateKeyExchangeAlgorithm(_context.Configuration.MemoryManager,
                                _context.Configuration.CryptographyConfiguration.KeyExchangeAlgorithm,
                                rpcCipher);
                        
                        handshakeRequest.EncryptionRequest = new EncryptionRequestProto();
                        handshakeRequest.EncryptionRequest.EncryptionAlgorithm = _context.Configuration
                            .CryptographyConfiguration.EncryptionAlgorithm.ToString();
                        handshakeRequest.EncryptionRequest.KeyExchangeAlgorithm = _context.Configuration
                            .CryptographyConfiguration.KeyExchangeAlgorithm.ToString();
                        handshakeRequest.EncryptionRequest.ClientKeyData = keyExchangeAlgorithm.GenerateClientKeyData();

                        combinedCts.Token.ThrowIfCancellationRequested();
                        RpcHandshakeResponseProto handshakeResponse =
                            await ExecuteInternalAsync<RpcHandshakeResponseProto>(HANDSHAKE_METHOD,
                                    handshakeRequest,
                                    ExecutionOptions.Default.WithCancellationToken(combinedCts.Token))
                                .ConfigureAwait(false);
                        keyExchangeAlgorithm.UpdateServerKeyData(handshakeResponse.EncryptionResponse
                                .ServerKeyData);

                        rpcCipher.SetKey(keyExchangeAlgorithm.GetKey());
                        await SendInternalAsync(SET_ENCRYPTION_METHOD, null,
                            SendingOptions.Default.WithCancellationToken(combinedCts.Token)).ConfigureAwait(false);
                        
                        _handshakeState.NegotiatedRpcCipher = rpcCipher;
                        base._cipher = rpcCipher;
                        _handshakeState._handshakeCompleted = true;
                        
                    }
                    catch
                    {
                        rpcCipher?.Dispose();
                        throw;
                    }
                }
                finally
                {
                    try
                    {
                        _systemSemaphore.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }
        
        public async Task Authenticate(AuthenticationInfo authenticationInfo, CancellationToken cancellationToken)
        {
            CheckDisposed();
            using (var combinedCts =
                   CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                       _context.Connection.CancellationToken))
            {
                if (authenticationInfo == null) throw new ArgumentNullException(nameof(authenticationInfo));
                await _systemSemaphore.WaitAsync(combinedCts.Token).ConfigureAwait(false);
                try
                {
                    _handshakeState.CheckCompleted(base._cipher);
                    _authState.CheckCanSend(_context.Connection.IsClientConnection);
                    combinedCts.Token.ThrowIfCancellationRequested();
                    var result = await ExecuteInternalAsync<Any>(AUTH_METHOD, authenticationInfo.Argument,
                        ExecutionOptions.Default.WithCancellationToken(combinedCts.Token)).ConfigureAwait(false);
                    _authState._authResult = result;
                    _authState._authState = authenticationInfo.State;
                    _authState._isAuthenticated = true;
                }
                finally
                {
                    try
                    {
                        _systemSemaphore.Release();
                    }
                    catch (ObjectDisposedException)
                    {
                    }
                }
            }
        }

        public async Task CreateUserSession(CancellationToken cancellationToken)
        {
            CheckDisposed();
            await _systemSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                await CreateUserSessionInternal(cancellationToken).ConfigureAwait(false);
            }
            finally
            {
                try
                {
                    _systemSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

        async Task CreateUserSessionInternal(CancellationToken cancellationToken)
        {
            CheckDisposed();
            try
            {
                using (var combinedCts =
                       CancellationTokenSource.CreateLinkedTokenSource(cancellationToken,
                           _context.Connection.CancellationToken))
                {
                    _handshakeState.CheckCompleted(base._cipher);
                    _authState.CheckCompleted();
                    combinedCts.Token.ThrowIfCancellationRequested();
                    RpcSessionBase session =
                        await _context.Configuration.SessionFactory.CreateSessionAsync(
                            new RpcSessionContext(this, _authState._authState, _context.Connection,
                                _authState._authResult, _context.Configuration.TaskScheduler,
                                _context.Configuration.LogManager),
                            combinedCts.Token).ConfigureAwait(false);
                    if (session == null)
                        throw new RpcException("Returned session is null", RpcResponseStatusCode.InvalidArgument);
                    combinedCts.Token.ThrowIfCancellationRequested();
                    // ReSharper disable once AccessToDisposedClosure
                    await Task.Factory.StartNew(() => session.OnInit(combinedCts.Token),
                        combinedCts.Token, TaskCreationOptions.None,
                        _context.Configuration.TaskScheduler ?? TaskScheduler.Default).Unwrap();
                    _userState._session = session;
                    _userState._rpcObjectScheme =
                        RpcObjectSchemeCache.TryGetObjectScheme(_context.Configuration.RpcInvocationRules,
                            session.GetType());

                    combinedCts.Token.ThrowIfCancellationRequested();
                }
            }
            catch (Exception ex)
            {
                // throw new RpcException($"Failed to create session: {ex.Message}");
                throw;
            }
        }

        protected override async Task<IMessage> OnRpcRequestExecute(RpcRequest request, CancellationToken cancellationToken)
        {
            CheckDisposed();
            if (request.Path.StartsWith(USER_METHOD_PREFIX))
                return await OnRpcRequestUserMethodExecute(request, cancellationToken);

            switch (request.Path)
            {
                case HANDSHAKE_METHOD:
                    return await OnHandshake(request, cancellationToken);
                case SET_ENCRYPTION_METHOD:
                    return await OnSetEncryption(request, cancellationToken);
                case AUTH_METHOD:
                    return await OnAuthenticate(request, cancellationToken);
                default:
                    throw new RpcException($"Unknown path {request.Path}", RpcResponseStatusCode.FailedPrecondition);
            }
        }
        
        async Task<IMessage> OnHandshake(RpcRequest request,
            CancellationToken cancellationToken)
        {
            await _systemSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _handshakeState.CheckCanReceive(_context.Connection.IsClientConnection);
                RpcHandshakeRequestProto handshakeRequest =
                    request.GetPayload<RpcHandshakeRequestProto>();

                if (handshakeRequest.ProtocolVersion != Constants.ProtocolVersion)
                    throw new RpcException(
                        $"Wrong protocol version {handshakeRequest.ProtocolVersion}, expected {Constants.ProtocolVersion}",
                        RpcResponseStatusCode.NotSupported);
                
                if (handshakeRequest.EncryptionRequest == null)
                {
                    if (_context.Configuration.CryptographyConfiguration.EncryptionAlgorithm != EncryptionAlgorithmEnum.None)
                        throw new RpcException($"Encryption required with {_context.Configuration.CryptographyConfiguration.EncryptionAlgorithm}",
                            RpcResponseStatusCode.InvalidOperation);
                    _handshakeState._handshakeCompleted = true;
                    return new RpcHandshakeResponseProto();
                }

                if (handshakeRequest.EncryptionRequest.EncryptionAlgorithm != _context.Configuration.CryptographyConfiguration.EncryptionAlgorithm.ToString())
                    throw new RpcException($"Encryption algorithm mismatch, required {_context.Configuration.CryptographyConfiguration.EncryptionAlgorithm}",
                        RpcResponseStatusCode.NotSupported);
                if (handshakeRequest.EncryptionRequest.KeyExchangeAlgorithm != _context.Configuration.CryptographyConfiguration.KeyExchangeAlgorithm.ToString())
                    throw new RpcException($"Key exchange algorithm mismatch, required {_context.Configuration.CryptographyConfiguration.EncryptionAlgorithm}",
                        RpcResponseStatusCode.NotSupported);

                IRpcCipher rpcCipher = null;
                try
                {
                    rpcCipher = CryptographyHelper.CreateCipher(_context.Configuration.CryptographyConfiguration
                        .EncryptionAlgorithm);
                    IKeyExchangeAlgorithm keyExchangeAlgorithm =
                        CryptographyHelper.CreateKeyExchangeAlgorithm(_context.Configuration.MemoryManager,
                            _context.Configuration.CryptographyConfiguration.KeyExchangeAlgorithm,
                            rpcCipher);
                    var serverKeyData = keyExchangeAlgorithm.KeyDataExchange(handshakeRequest.EncryptionRequest.ClientKeyData);

                    RpcHandshakeResponseProto handshakeResponse = new RpcHandshakeResponseProto();
                    handshakeResponse.EncryptionResponse = new EncryptionResponseProto();
                    handshakeResponse.EncryptionResponse.ServerKeyData = serverKeyData;

                    rpcCipher.SetKey(keyExchangeAlgorithm.GetKey());
                    _handshakeState.NegotiatedRpcCipher = rpcCipher;
                    _handshakeState._handshakeCompleted = true;

                    return handshakeResponse;
                }
                catch
                {
                    rpcCipher?.Dispose();
                    throw;
                }
            }
            finally
            {
                try
                {
                    _systemSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
        
        
        async Task<IMessage> OnSetEncryption(RpcRequest request, CancellationToken cancellationToken)
        {
            await _systemSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _cipher = _handshakeState.GetCipher();
                return null;
            }
            finally
            {
                try
                {
                    _systemSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }
        
        
        async Task<Any> OnAuthenticate(RpcRequest request, CancellationToken cancellationToken)
        {
            await _systemSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            try
            {
                _handshakeState.CheckCompleted(base._cipher);
                _authState.CheckCanReceive(_context.Connection.IsClientConnection);
                
                cancellationToken.ThrowIfCancellationRequested();
                Any arg = null;
                if (request.HasPayload)
                    arg = request.GetPayload<Any>();

                var context = new AuthenticationContext(
                    _context.Connection,
                    arg);
                await _context.Configuration.SessionFactory.AuthenticateAsync(context, cancellationToken)
                    .ConfigureAwait(false);

                _authState._authState = context.AuthenticationState;
                _authState._authResult = context.AuthenticationResult;
                _authState._isAuthenticated = true;

                await CreateUserSessionInternal(cancellationToken).ConfigureAwait(false);
                
                return context.AuthenticationResult;
            }
            finally
            {
                try
                {
                    _systemSemaphore.Release();
                }
                catch (ObjectDisposedException)
                {
                }
            }
        }

       
        async Task<IMessage> OnRpcRequestUserMethodExecute(RpcRequest request, CancellationToken cancellationToken)
        {
            _userState.CheckInitialized();
            _userState._session.CheckClosedInternal();

            string methodName = request.Path.Substring(1);
            var container = _userState._rpcObjectScheme.GetInvocationContainer(methodName);

            IMessage result;
            if (container.ArgumentDescriptor != null)
            {
                if (!request.HasPayload)
                    throw new RpcException(
                        $"Method requires an argument of type {container.ArgumentDescriptor.ClrType.FullName}",
                        RpcResponseStatusCode.InvalidArgument);
                IMessage argument = request.GetPayload(container.ArgumentDescriptor.Parser);
                result = await _userState._session.LocalExecutionInterceptor(
                    container.InvokeAsync(_userState._session, argument, cancellationToken), methodName, argument,
                    cancellationToken).ConfigureAwait(false);
            }
            else
                result = await _userState._session
                    .LocalExecutionInterceptor(container.InvokeAsync(_userState._session, null, cancellationToken),
                        methodName, null, cancellationToken).ConfigureAwait(false);

            if (container.ResultDescriptor != null && result == null)
                throw new RpcException("Method returned a null value", RpcResponseStatusCode.InvalidArgument);
            
            cancellationToken.ThrowIfCancellationRequested();
            
            return result;
        }

        internal void PollEvents()
        {
            CheckDisposed();
            if (!_authState._isAuthenticated && (DateTimeOffset.UtcNow - _authState._started).TotalSeconds >= 5)
            {
                _logger.Debug($"{GetLogsSign()} unauthenticated idle connection. Closing...");
                _context.Connection.Close();
            }
        }

        public async Task SendUserMethodInternalAsync(string method, IMessage arg, SendingOptions sendingOptions)
        {
            await base.SendInternalAsync(USER_METHOD_PREFIX + method, arg, sendingOptions);
        }

        public async Task<T> ExecuteUserMethodInternalAsync<T>(string method, IMessage arg, ExecutionOptions options)
            where T : IMessage<T>, new()
        {
            return await base.ExecuteInternalAsync<T>(USER_METHOD_PREFIX + method, arg, options);
        }
    }
}
