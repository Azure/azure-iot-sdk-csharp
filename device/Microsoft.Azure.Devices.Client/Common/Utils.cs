// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.IO;
    using System.Text;
#if !NETMF
    using Newtonsoft.Json;
#endif
    using Extensions;

    static class Utils
    {
        static Utils()
        {
        }

#if !PCL && !NETMF
        public static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            ValidateBufferBounds(buffer.Length, offset, size);
        }
        static void ValidateBufferBounds(int bufferSize, int offset, int size)
        {
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, Common.Resources.ArgumentMustBeNonNegative);
            }

            if (offset > bufferSize)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), offset, Common.Resources.OffsetExceedsBufferSize.FormatInvariant(bufferSize));
            }

            if (size <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, Common.Resources.ArgumentMustBePositive);
            }

            int remainingBufferSpace = bufferSize - offset;
            if (size > remainingBufferSpace)
            {
                throw new ArgumentOutOfRangeException(nameof(size), size, Common.Resources.SizeExceedsRemainingBufferSpace.FormatInvariant(remainingBufferSpace));
            }
        }
#endif
        public static string GetClientVersion()
        {
            // This string is picked up by the bump_version script (https://github.com/Azure/azure-iot-sdks/blob/develop/build/release/bump_version/inputs.js)
            var CommonAssemblyVersion = "1.5.2";
            return "Microsoft.Azure.Devices.Client/" + CommonAssemblyVersion;
        }

        public static DeliveryAcknowledgement ConvertDeliveryAckTypeFromString(string value)
        {
            switch (value)
            {
                case "none":
                    return DeliveryAcknowledgement.None;
                case "negative":
                    return DeliveryAcknowledgement.NegativeOnly;
                case "positive":
                    return DeliveryAcknowledgement.PositiveOnly;
                case "full":
                    return DeliveryAcknowledgement.Full;

                default:
                    throw new NotSupportedException("Unknown value: '" + value + "'");
            }
        }

        public static string ConvertDeliveryAckTypeToString(DeliveryAcknowledgement value)
        {
            switch (value)
            {
                case DeliveryAcknowledgement.None:
                    return "none";
                case DeliveryAcknowledgement.NegativeOnly:
                    return "negative";
                case DeliveryAcknowledgement.PositiveOnly:
                    return "positive";
                case DeliveryAcknowledgement.Full:
                    return "full";

                default:
                    throw new NotSupportedException("Unknown value: '" + value + "'");
            }
        }

#if !NETMF
        public static void ValidateDataIsEmptyOrJson(byte[] data)
        {
            if (data.Length != 0)
            {
                var stream = new MemoryStream(data);
                var streamReader = new StreamReader(stream, Encoding.UTF8, false, Math.Min(1024, data.Length));

                using (var reader = new JsonTextReader(streamReader))
                {
                    while (reader.Read())
                    {
                    }
                }
            }
        }
#endif
    }
}