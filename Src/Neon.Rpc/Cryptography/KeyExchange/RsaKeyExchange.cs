using System;
using System.IO;
using System.Security.Cryptography;
using Google.Protobuf;
using Neon.Util;
using Neon.Util.Pooling;

namespace Neon.Rpc.Cryptography.KeyExchange
{
    class RsaKeyExchange : IKeyExchangeAlgorithm
    {
        public KeyExchangeStatus Status { get; private set; }
        public int KeySize => _keySize;
        
        static RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider();
        
        RSACryptoServiceProvider _rsaCryptoServiceProvider;
        readonly IMemoryManager _memoryManager;

        int _keySize;
        int _commonKeySize;
        byte[] _commonKey;
    
        public RsaKeyExchange(IMemoryManager memoryManager, int keySize, int commonKeySize)
        {
            _memoryManager = memoryManager ?? throw new ArgumentNullException(nameof(memoryManager));
            _keySize = keySize;
            _commonKeySize = commonKeySize;
        }
    
        public void Dispose()
        {
            _rsaCryptoServiceProvider?.Dispose();
        }
    
        ByteString SerializeRsaParameters(RSAParameters parameters)
        {
            using (var stream = _memoryManager.GetStream(Guid.NewGuid()))
            {
                using (var writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, true))
                {
                    // VarIntBitConverterNet.WriteVarintBytes(writer, parameters.Q.Length);
                    // writer.Write(parameters.Q);
                    // VarIntBitConverterNet.WriteVarintBytes(writer, parameters.D.Length);
                    // writer.Write(parameters.D);
                    // VarIntBitConverterNet.WriteVarintBytes(writer, parameters.P.Length);
                    // writer.Write(parameters.P);
                    // VarIntBitConverterNet.WriteVarintBytes(writer, parameters.DP.Length);
                    // writer.Write(parameters.DP);
                    VarIntBitConverterNet.WriteVarintBytes(writer, parameters.Exponent.Length);
                    writer.Write(parameters.Exponent);
                    VarIntBitConverterNet.WriteVarintBytes(writer, parameters.Modulus.Length);
                    writer.Write(parameters.Modulus);
                    // VarIntBitConverterNet.WriteVarintBytes(writer, parameters.DQ.Length);
                    // writer.Write(parameters.DQ);
                    // VarIntBitConverterNet.WriteVarintBytes(writer, parameters.InverseQ.Length);
                    // writer.Write(parameters.InverseQ);
                    
                    stream.Position = 0;
                    return ByteString.FromStream(stream);
                }
            }
        }
    
        RSAParameters DeserializeRsaParameters(ByteString data)
        {
            RSAParameters result = new RSAParameters();
            using (var stream = _memoryManager.GetStream(data.Length, Guid.NewGuid()))
            {
                data.WriteTo(stream);
                stream.Position = 0;
                using (var reader = new BinaryReader(stream, System.Text.Encoding.UTF8, true))
                {
                    // int qLen = VarIntBitConverterNet.ToInt32(reader);
                    // result.Q = reader.ReadBytes(qLen);
                    // int dLen = VarIntBitConverterNet.ToInt32(reader);
                    // result.D = reader.ReadBytes(dLen);
                    // int pLen = VarIntBitConverterNet.ToInt32(reader);
                    // result.P = reader.ReadBytes(pLen);
                    // int dpLen = VarIntBitConverterNet.ToInt32(reader);
                    // result.DP = reader.ReadBytes(dpLen);
                    int expLen = VarIntBitConverterNet.ToInt32(reader);
                    result.Exponent = reader.ReadBytes(expLen);
                    int modLen = VarIntBitConverterNet.ToInt32(reader);
                    result.Modulus = reader.ReadBytes(modLen);
                    // int DqLen = VarIntBitConverterNet.ToInt32(reader);
                    // result.DQ = reader.ReadBytes(DqLen);
                    // int iqLen = VarIntBitConverterNet.ToInt32(reader);
                    // result.InverseQ = reader.ReadBytes(iqLen);

                    return result;
                }
            }
        }
        
        public ByteString GenerateClientKeyData()
        {
            if (Status != KeyExchangeStatus.Initial || _rsaCryptoServiceProvider != null)
                throw new InvalidOperationException($"Wrong status {Status}, expected {KeyExchangeStatus.Initial}");
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider(_keySize);
            var result = SerializeRsaParameters(_rsaCryptoServiceProvider.ExportParameters(false));
            Status = KeyExchangeStatus.ClientKeyDataGenerated;
            return result;
        }

        public ByteString KeyDataExchange(ByteString keyData)
        {
            if (Status != KeyExchangeStatus.Initial || _rsaCryptoServiceProvider != null)
                throw new InvalidOperationException($"Wrong status {Status}, expected {KeyExchangeStatus.Initial}");
            _rsaCryptoServiceProvider = new RSACryptoServiceProvider(_keySize);
            _rsaCryptoServiceProvider.ImportParameters(DeserializeRsaParameters(keyData));
            _commonKey = new byte[_commonKeySize / 8];
            rngCsp.GetBytes(_commonKey);
            var result = _rsaCryptoServiceProvider.Encrypt(_commonKey, RSAEncryptionPadding.Pkcs1);
            Status = KeyExchangeStatus.CommonKeySet;
            return ByteString.CopyFrom(result);
        }

        public void UpdateServerKeyData(ByteString keyData)
        {
            if (Status != KeyExchangeStatus.ClientKeyDataGenerated)
                throw new InvalidOperationException(
                    $"Wrong status {Status}, expected {KeyExchangeStatus.ClientKeyDataGenerated}");
            _commonKey = _rsaCryptoServiceProvider.Decrypt(keyData.ToByteArray(), RSAEncryptionPadding.Pkcs1);
            if (_commonKey.Length*8 != _commonKeySize)
                throw new InvalidOperationException($"Wrong key size {_commonKey.Length}, expected {_commonKeySize}");
            Status = KeyExchangeStatus.CommonKeySet;
        }

        public byte[] GetKey()
        {
            if (Status != KeyExchangeStatus.CommonKeySet)
                throw new InvalidOperationException($"Wrong status {Status}, expected {KeyExchangeStatus.CommonKeySet}");
            return _commonKey;
        }
    }
}