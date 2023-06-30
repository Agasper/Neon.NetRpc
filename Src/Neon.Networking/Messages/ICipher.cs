using System.IO;

namespace Neon.Networking.Messages
{
    public interface ICipher
    {
        void Encrypt(Stream source, Stream destination, byte[] buffer);
        void Decrypt(Stream source, Stream destination, byte[] buffer);
    }
}