using System.IO;

namespace Neon.Networking.Messages
{
    public interface ICipher
    {
        bool IsKeySet { get; }
        void Encrypt(Stream source, Stream destination, byte[] buffer);
        void Decrypt(Stream source, Stream destination, byte[] buffer);
    }
}