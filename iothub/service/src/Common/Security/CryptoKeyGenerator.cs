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
    /// Utility methods for generating cryptographically secure keys and passwords
    /// </summary>
    static public class CryptoKeyGenerator
    {
#if NET451
        const int DefaultPasswordLength = 16;
        const int GuidLength = 16;
#endif

        /// <summary>
        /// Size of the SHA 512 key
        /// </summary>
        public const int Sha512KeySize = 64;

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
        /// Generates a key of the specified size
        /// </summary>
        /// <param name="keySize">Desired key size</param>
        /// <returns>A generated key</returns>
        public static string GenerateKey(int keySize)
        {
            return Convert.ToBase64String(GenerateKeyBytes(keySize));
        }

#if NET451
        public static string GenerateKeyInHex(int keySize)
        {
            var keyBytes = new byte[keySize];
            using (var cyptoProvider = new RNGCryptoServiceProvider())
            {
                cyptoProvider.GetNonZeroBytes(keyBytes);
            }
            return BitConverter.ToString(keyBytes).Replace("-", "");
        }

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

        public static string GeneratePassword()
        {
            return GeneratePassword(DefaultPasswordLength, false);
        }

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
