// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Security.Cryptography;

#if NET451

using System.Web.Security;

#endif

#if !NET451

using System.Linq;

#endif

namespace Microsoft.Azure.Devices.Common
{
    /// <summary>
    /// Utility methods for generating cryptographically secure keys and passwords.
    /// </summary>
    public static class CryptoKeyGenerator
    {
#if NET451
        private const int DefaultPasswordLength = 16;
        private const int GuidLength = 16;
#endif

        /// <summary>
        /// Size of the SHA 512 key.
        /// </summary>
        public const int Sha512KeySize = 64;

        /// <summary>
        /// Generate a key with a specified key size.
        /// </summary>
        /// <param name="keySize">The size of the key.</param>
        /// <returns>Byte array representing the key.</returns>
        [Obsolete("This method will be deprecated in a future version.")]
        public static byte[] GenerateKeyBytes(int keySize)
        {
#if NET451
            byte[] keyBytes = new byte[keySize];
            using var cyptoProvider = new RNGCryptoServiceProvider();
            cyptoProvider.GetNonZeroBytes(keyBytes);
#else
            byte[] keyBytes = new byte[keySize];
            using var cyptoProvider = RandomNumberGenerator.Create();
            while (keyBytes.Contains(byte.MinValue))
            {
                cyptoProvider.GetBytes(keyBytes);
            }
#endif
            return keyBytes;
        }

        /// <summary>
        /// Generates a key of the specified size.
        /// </summary>
        /// <param name="keySize">Desired key size.</param>
        /// <returns>A generated key.</returns>
        [Obsolete("This method will be deprecated in a future version.")]
        public static string GenerateKey(int keySize)
        {
            return Convert.ToBase64String(GenerateKeyBytes(keySize));
        }

#if NET451
        /// <summary>
        /// Generate a hexadecimal key of the specified size.
        /// </summary>
        /// <param name="keySize">Desired key size.</param>
        /// <returns>A generated hexadecimal key.</returns>
        [Obsolete("This method will not be carried forward to newer .NET targets.")]
        public static string GenerateKeyInHex(int keySize)
        {
            byte[] keyBytes = new byte[keySize];
            using var cyptoProvider = new RNGCryptoServiceProvider();
            cyptoProvider.GetNonZeroBytes(keyBytes);

            return BitConverter.ToString(keyBytes).Replace("-", "");
        }

        /// <summary>
        /// Generate a GUID using random bytes from the framework's cryptograpically strong RNG (Random Number Generator).
        /// </summary>
        /// <returns>A cryptographically secure GUID.</returns>
        [Obsolete("This method will not be carried forward to newer .NET targets.")]
        public static Guid GenerateGuid()
        {
            byte[] bytes = new byte[GuidLength];
            using var rng = new RNGCryptoServiceProvider();
            rng.GetBytes(bytes);

            uint time = BitConverter.ToUInt32(bytes, 0);
            ushort timeMid = BitConverter.ToUInt16(bytes, 4);
            ushort timeHiAndVer = BitConverter.ToUInt16(bytes, 6);
            timeHiAndVer = (ushort)((timeHiAndVer | 0x4000) & 0x4FFF);

            bytes[8] = (byte)((bytes[8] | 0x80) & 0xBF);

            return new Guid(
                time,
                timeMid,
                timeHiAndVer,
                bytes[8],
                bytes[9],
                bytes[10],
                bytes[11],
                bytes[12],
                bytes[13],
                bytes[14],
                bytes[15]);
        }

        /// <summary>
        /// Generate a unique password with a default length and without converting it to Base64String.
        /// </summary>
        /// <returns>A unique password.</returns>
        [Obsolete("This method will not be carried forward to newer .NET targets.")]
        public static string GeneratePassword()
        {
            return GeneratePassword(DefaultPasswordLength, false);
        }

        /// <summary>
        /// Generate a unique password with a specific length and a flag to indicate whether to encode the password.
        /// </summary>
        /// <param name="length">Desired length of the password.</param>
        /// <param name="base64Encoding">Encode the password if set to True. False otherwise.</param>
        /// <returns>A generated password.</returns>
        [Obsolete("This method will not be carried forward to newer .NET targets.")]
        public static string GeneratePassword(int length, bool base64Encoding)
        {
            string password = Membership.GeneratePassword(length, length / 2);
            if (base64Encoding)
            {
                password = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            }
            return password;
        }
#endif
    }
}
