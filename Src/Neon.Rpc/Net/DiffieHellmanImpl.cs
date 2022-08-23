using System;
using System.Numerics;
using System.Security.Cryptography;
using Neon.Networking.Cryptography;
using Neon.Networking.Messages;

namespace Neon.Rpc.Net
{
    public class DiffieHellmanImpl
    {
        public enum DhStatus
        {
            None,
            WaitingForServerMod,
            CommonKeySet
        }

        public DhStatus Status => status;
        public byte[] CommonKey => commonKey;
        
        static readonly BigInteger mod = BigInteger.Parse("8344036200867339188401421868243599800768302958029168098393701372645433245359142296592083846452559047641776847523169623760010321326001893234240700419675989");
        static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        const ushort mark = 21078;
        
        BigInteger privateKey;
        BigInteger publicKey;
        byte[] commonKey;

        DhStatus status;
        ICipher cipher;

        public DiffieHellmanImpl(ICipher cipher)
        {
            this.cipher = cipher;
        }

        public void SendHandshakeRequest(IRawMessage handshakeRequest)
        {
            if (handshakeRequest == null) throw new ArgumentNullException(nameof(handshakeRequest));
            if (status != DhStatus.None)
                throw new InvalidOperationException($"Wrong status {status}, expected: {DhStatus.None}");

            byte[] privateKeyBytes = new byte[64];
            byte[] publicKeyBytes = new byte[64];
            rngCsp.GetBytes(privateKeyBytes);
            rngCsp.GetBytes(publicKeyBytes);
            privateKey = new BigInteger(privateKeyBytes);
            publicKey = new BigInteger(publicKeyBytes);

            BigInteger clientMod = ((privateKey * publicKey) % mod);

            handshakeRequest.Write(mark);
            handshakeRequest.Write(publicKeyBytes.Length);
            handshakeRequest.Write(publicKeyBytes);
            byte[] clientModBytes = clientMod.ToByteArray();
            handshakeRequest.Write(clientModBytes.Length);
            handshakeRequest.Write(clientModBytes);
            
            status = DhStatus.WaitingForServerMod;
        }

        public void RecvHandshakeRequest(IRawMessage handshakeRequest, IRawMessage handshakeResponse)
        {
            if (handshakeRequest == null) throw new ArgumentNullException(nameof(handshakeRequest));
            if (handshakeResponse == null) throw new ArgumentNullException(nameof(handshakeResponse));
            if (status != DhStatus.None)
                throw new InvalidOperationException($"Wrong status {status}, expected: {DhStatus.None}");

            BigInteger clientMod;
            
            ushort gotMark = handshakeRequest.ReadUInt16();
            if (mark != gotMark)
                throw new InvalidOperationException(
                    "Handshake failed, wrong mark. Perhaps the other peer is trying to connect with unsecure connection");
            int publicKeySize = handshakeRequest.ReadInt32();
            publicKey = new BigInteger(handshakeRequest.ReadBytes(publicKeySize));
            int clientModSize = handshakeRequest.ReadInt32();
            clientMod = new BigInteger(handshakeRequest.ReadBytes(clientModSize));
            
            byte[] keyBytes = new byte[64];
            rngCsp.GetBytes(keyBytes);
            privateKey = new BigInteger(keyBytes);
            BigInteger serverMod = (privateKey * publicKey) % mod;
            commonKey = ((privateKey * clientMod) % mod).ToByteArray();
            
            byte[] serverModBytes = serverMod.ToByteArray();
            handshakeResponse.Write(serverModBytes.Length);
            handshakeResponse.Write(serverModBytes);
            
            SetCipherKey();
        }

        public void RecvHandshakeResponse(IRawMessage handshakeResponse)
        {
            if (status != DhStatus.WaitingForServerMod)
                throw new InvalidOperationException($"Wrong status {status}, expected: {DhStatus.WaitingForServerMod}");

            int serverModSize = handshakeResponse.ReadInt32();
            BigInteger serverMod = new BigInteger(handshakeResponse.ReadBytes(serverModSize));
            commonKey = ((privateKey * serverMod) % mod).ToByteArray();

            SetCipherKey();
        }

        void SetCipherKey()
        {
            if (commonKey == null)
                throw new InvalidOperationException("Handshake sequence has not been completed. Couldn't set cipher key");

            if (cipher.KeySize == 128)
            {
                using (MD5 hash = MD5.Create())
                    cipher.SetKey(hash.ComputeHash(commonKey));
            }
            else
                throw new InvalidOperationException($"{nameof(DiffieHellmanImpl)} supports only ciphers with 128 bit keys");

            status = DhStatus.CommonKeySet;
        }
    }
}