// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal static class MessageHelpers
    {
        /// <summary>
        /// Reported max of entire payload is 256k, but we'll hold back 1 k as fudge factor for things we can't calculate.
        /// </summary>
        public const int MaxMessagePayloadBytes = 256 * 1024 - 1024;

        public static int GetMessagePayloadSize(TelemetryMessage message)
        {
            object payload = message.Payload;
            int size = ObjectToByteArray(payload).Length;

            return size;
        }

        private static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
            {
                return null;
            }

            var bf = new BinaryFormatter();
            using var ms = new MemoryStream();
#pragma warning disable SYSLIB0011 // Type or member is obsolete
            bf.Serialize(ms, obj);
#pragma warning restore SYSLIB0011 // Type or member is obsolete
            return ms.ToArray();
        }
    }
}
