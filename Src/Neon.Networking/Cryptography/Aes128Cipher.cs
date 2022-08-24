﻿using System;
using System.Security.Cryptography;

namespace Neon.Networking.Cryptography
{
    public class Aes128Cipher : AesBaseCipher
    {
        public Aes128Cipher() : base()
        {
            cipher.KeySize = 128;
        }
    }
}
