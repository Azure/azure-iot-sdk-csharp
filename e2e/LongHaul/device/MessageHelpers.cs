using Microsoft.Azure.Devices.Client;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace Microsoft.Azure.IoT.Thief.Device
{
    internal static class MessageHelpers
    {
        /// <summary>
        /// Reported max of entire payload is 256k, but we'll hold back 1 k as fudge factor for things we can't calculate.
        /// </summary>
        public const int MaxMessagePayloadBytes = (256 * 1024) - 1024;

        public static int GetMessagePayloadSize(TelemetryMessage message)
        {
            var payload = message.Payload;
            int size = ObjectToByteArray(payload).Length;

            return size;
        }

        private static byte[] ObjectToByteArray(object obj)
        {
            if (obj == null)
                return null;
            BinaryFormatter bf = new BinaryFormatter();
            using (MemoryStream ms = new MemoryStream())
            {
                bf.Serialize(ms, obj);
                return ms.ToArray();
            }
        }
    }
}
