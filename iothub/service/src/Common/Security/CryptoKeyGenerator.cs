// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System;
using System.Text;

#if !NET451

using System.Security.Cryptography;

#else
    using System.Web.Security;
    using System.Security.Cryptography;
#endif

namespace Microsoft.Azure.Devices.Common
{
    /// <summary>
    /// Utility methods for generating cryptographically secure keys and passwords.
    /// </summary>
    static public class CryptoKeyGenerator
    {
#if NET451
        const int DefaultPasswordLength = 16;
        const int GuidLength = 16;
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
        public static byte[] GenerateKeyBytes(int keySize)
        {
#if !NET451
            var keyBytes = new byte[keySize];
            using (var cyptoProvider = RandomNumberGenerator.Create())
            {
                while (keyBytes.Contains(byte.MinValue))
                {
                    cyptoProvider.GetBytes(keyBytes);
                }
            }
#else
            var keyBytes = new byte[keySize];
            using (var cyptoProvider = new RNGCryptoServiceProvider())
            {
                cyptoProvider.GetNonZeroBytes(keyBytes);
            }
#endif
            return keyBytes;
        }

        /// <summary>
        /// Generates a key of the specified size.
        /// </summary>
        /// <param name="keySize">Desired key size.</param>
        /// <returns>A generated key.</returns>
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
        public static string GenerateKeyInHex(int keySize)
        {
            var keyBytes = new byte[keySize];
            using (var cyptoProvider = new RNGCryptoServiceProvider())
            {
                cyptoProvider.GetNonZeroBytes(keyBytes);
            }
            return BitConverter.ToString(keyBytes).Replace("-", "");
        }

        /// <summary>
        /// Generate a unique GUID.
        /// </summary>
        /// <returns>A GUID generated using random bytes from the framework's cryptograpically strong RNG (Random Number Generator).</returns>
        public static Guid GenerateGuid()
        {
            byte[] bytes = new byte[GuidLength];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(bytes);
            }

            var time = BitConverter.ToUInt32(bytes, 0);
            var time_mid = BitConverter.ToUInt16(bytes, 4);
            var time_hi_and_ver = BitConverter.ToUInt16(bytes, 6);
            time_hi_and_ver = (ushort)((time_hi_and_ver | 0x4000) & 0x4FFF);

            bytes[8] = (byte)((bytes[8] | 0x80) & 0xBF);

            return new Guid(time, time_mid, time_hi_and_ver, bytes[8], bytes[9],
                bytes[10], bytes[11], bytes[12], bytes[13], bytes[14], bytes[15]);
        }

        /// <summary>
        /// Generate a unique password with a default length and without converting it to Base64String.
        /// </summary>
        /// <returns>A unique password.</returns>
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
        public static string GeneratePassword(int length, bool base64Encoding)
        {
            var password = Membership.GeneratePassword(length, length / 2);
            if (base64Encoding)
            {
                password = Convert.ToBase64String(Encoding.UTF8.GetBytes(password));
            }
            return password;
        }
#endif
    }
}
