using System;
using System.Security.Cryptography;

namespace Neon.Networking.Cryptography
{
    public class Aes256Cipher : AesBaseCipher
    {
        public Aes256Cipher() : base()
        {
            cipher.KeySize = 256;
        }
    }
}
