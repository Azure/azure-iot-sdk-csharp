using Microsoft.Azure.Devices.Client;
using System;
using System.Collections.Generic;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal static class MessageHelpers
    {
        /// <summary>
        /// Reported max of entire payload is 256k, but we'll hold back 1 k as fudge factor for things we can't calculate.
        /// </summary>
        public const int MaxMessagePayloadBytes = (256 * 1024) - 1024;

        private const int Iso8601Length = 24;

        public static int GetMessagePayloadSize(Message message)
        {
            int size = (int)message.BodyStream.Length;
            
            foreach (KeyValuePair<string, string> prop in message.Properties)
            {
                size += prop.Key.Length;
                size += prop.Value.Length;
            }

            size += message.MessageId?.Length ?? 0;
            size += message.ContentType?.Length ?? 0;
            size += message.ContentEncoding?.Length ?? 0;
            size += message.CorrelationId?.Length ?? 0;
            size += message.UserId?.Length ?? 0;
            size += message.To?.Length ?? 0;
            size += message.ExpiryTimeUtc == DateTime.MinValue ? 0 : Iso8601Length;

            return size;
        }
    }
}
