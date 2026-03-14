// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Security.Cryptography;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    /// <summary>
    /// Utility methods for generating cryptographically secure keys and passwords.
    /// </summary>
    internal static class CryptoKeyGenerator
    {
        /// <summary>
        /// Size of the SHA 512 key.
        /// </summary>
        internal const int Sha512KeySize = 64;

        /// <summary>
        /// Generate a key with a specified key size.
        /// </summary>
        /// <param name="keySize">The size of the key.</param>
        /// <returns>Byte array representing the key.</returns>
        internal static byte[] GenerateKeyBytes(int keySize)
        {

            byte[] keyBytes = new byte[keySize];
            using var cyptoProvider = RandomNumberGenerator.Create();
            while (keyBytes.Contains(byte.MinValue))
            {
                cyptoProvider.GetBytes(keyBytes);
            }
            return keyBytes;
        }

        /// <summary>
        /// Generates a key of the specified size.
        /// </summary>
        /// <param name="keySize">Desired key size.</param>
        /// <returns>A generated key.</returns>
        internal static string GenerateKey(int keySize)
        {
            return Convert.ToBase64String(GenerateKeyBytes(keySize));
        }
    }
}
