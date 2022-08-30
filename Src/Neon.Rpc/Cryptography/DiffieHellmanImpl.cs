using System;
using System.Numerics;
using System.Security.Cryptography;
using Neon.Networking.Cryptography;
using Neon.Networking.Messages;

namespace Neon.Rpc.Cryptography
{
    class DiffieHellmanImpl
    {
        public enum DhStatus
        {
            None,
            WaitingForServerMod,
            CommonKeySet
        }

        public DhStatus Status => status;
        public byte[] CommonKey => commonKey;
        
        static readonly byte[] modBytes = new byte[]
        {
            13, 151, 195, 185, 79, 32, 124, 97, 123, 150, 99, 206, 207, 207, 201, 254, 143, 90, 147, 222, 148, 164, 48,
            164, 119, 58, 133, 163, 82, 105, 160, 194, 151, 61, 90, 152, 3, 50, 145, 125, 14, 94, 124, 38, 45, 99, 26,
            46, 213, 117, 213, 173, 24, 217, 222, 84, 204, 178, 31, 95, 190, 206, 66, 255, 246, 241, 173, 198, 172, 25,
            100, 195, 74, 54, 168, 181, 57, 139, 254, 71, 92, 132, 58, 150, 235, 12, 26, 169, 226, 240, 97, 187, 118,
            53, 201, 150, 112, 126, 228, 116, 102, 32, 21, 129, 39, 230, 200, 209, 29, 87, 148, 190, 14, 45, 126, 109,
            224, 36, 207, 166, 226, 189, 65, 115, 200, 238, 107, 123, 195, 198, 227, 186, 42, 217, 106, 113, 76, 50, 46,
            254, 13, 118, 132, 150, 133, 232, 154, 143, 209, 25, 106, 61, 1, 184, 254, 224, 91, 142, 146, 137, 197, 213,
            86, 199, 110, 186, 29, 190, 214, 43, 23, 37, 236, 31, 25, 39, 2, 240, 209, 175, 227, 32, 123, 37, 222, 54,
            30, 60, 98, 188, 149, 165, 76, 137, 12, 241, 106, 52, 34, 138, 102, 147, 133, 104, 53, 198, 245, 161, 72,
            215, 239, 94, 47, 43, 252, 19, 155, 5, 200, 237, 22, 156, 55, 204, 156, 54, 24, 234, 4, 226, 226, 191, 8,
            204, 216, 191, 244, 245, 101, 70, 168, 61, 102, 44, 34, 109, 75, 163, 43, 116, 180, 117, 241, 69, 159, 12
        };
        
        static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        const ushort token = 21078;
        
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

            int size = modBytes.Length;

            byte[] privateKeyBytes = new byte[size];
            byte[] publicKeyBytes = new byte[size];
            rngCsp.GetBytes(privateKeyBytes);
            rngCsp.GetBytes(publicKeyBytes);
            privateKey = new BigInteger(privateKeyBytes);
            publicKey = new BigInteger(publicKeyBytes);

            BigInteger mod = new BigInteger(modBytes);
            BigInteger clientMod = ((privateKey * publicKey) % mod);

            handshakeRequest.Write(token);
            handshakeRequest.WriteVarInt(publicKeyBytes.Length);
            handshakeRequest.Write(publicKeyBytes);
            byte[] clientModBytes = clientMod.ToByteArray();
            handshakeRequest.WriteVarInt(clientModBytes.Length);
            handshakeRequest.Write(clientModBytes);
            
            status = DhStatus.WaitingForServerMod;
        }

        public void RecvHandshakeRequest(IRawMessage handshakeRequest, IRawMessage handshakeResponse)
        {
            if (handshakeRequest == null) 
                throw new ArgumentNullException(nameof(handshakeRequest));
            if (handshakeResponse == null)
                throw new ArgumentNullException(nameof(handshakeResponse));
            if (status != DhStatus.None)
                throw new InvalidOperationException($"Wrong status {status}, expected: {DhStatus.None}");
            
            ushort gotToken = handshakeRequest.ReadUInt16();
            if (token != gotToken)
                throw new InvalidOperationException(
                    "Handshake failed, wrong mark. Perhaps the other peer is trying to connect with unsecure connection");
            int publicKeySize = handshakeRequest.ReadVarInt32();
            publicKey = new BigInteger(handshakeRequest.ReadBytes(publicKeySize));
            int clientModSize = handshakeRequest.ReadVarInt32();
            BigInteger clientMod = new BigInteger(handshakeRequest.ReadBytes(clientModSize));

            BigInteger mod = new BigInteger(modBytes);
            int size = modBytes.Length;
            
            byte[] keyBytes = new byte[size];
            rngCsp.GetBytes(keyBytes);
            privateKey = new BigInteger(keyBytes);
            BigInteger serverMod = (privateKey * publicKey) % mod;
            commonKey = ((privateKey * clientMod) % mod).ToByteArray();
            
            byte[] serverModBytes = serverMod.ToByteArray();
            handshakeResponse.WriteVarInt(serverModBytes.Length);
            handshakeResponse.Write(serverModBytes);
            
            SetCipherKey();
        }

        public void RecvHandshakeResponse(IRawMessage handshakeResponse)
        {
            if (status != DhStatus.WaitingForServerMod)
                throw new InvalidOperationException($"Wrong status {status}, expected: {DhStatus.WaitingForServerMod}");
            
            BigInteger mod = new BigInteger(modBytes);

            int serverModSize = handshakeResponse.ReadVarInt32();
            BigInteger serverMod = new BigInteger(handshakeResponse.ReadBytes(serverModSize));
            commonKey = ((privateKey * serverMod) % mod).ToByteArray();

            SetCipherKey();
        }

        void SetCipherKey()
        {
            if (commonKey == null)
                throw new InvalidOperationException(
                    "Handshake sequence has not been completed. Couldn't set cipher key");

            switch (cipher.KeySize)
            {
                case 128:
                    using (MD5 hash = MD5.Create())
                        cipher.SetKey(hash.ComputeHash(commonKey));
                    break;
                case 256:
                    using (SHA256 hash = SHA256.Create())
                        cipher.SetKey(hash.ComputeHash(commonKey));
                    break;
                case 384:
                    using (SHA384 hash = SHA384.Create())
                        cipher.SetKey(hash.ComputeHash(commonKey));
                    break;
                case 512:
                    using (SHA512 hash = SHA512.Create())
                        cipher.SetKey(hash.ComputeHash(commonKey));
                    break;
                default:
                    throw new InvalidOperationException("Only allowed ciphers with key size: 128, 256, 512");
            }
            
            status = DhStatus.CommonKeySet;
        }
    }
}