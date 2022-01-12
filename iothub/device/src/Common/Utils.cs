// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.Azure.Devices.Client.Extensions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client
{
    internal static class Utils
    {
        static Utils()
        {
        }

        public static void ValidateBufferBounds(byte[] buffer, int offset, int size)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }

            ValidateBufferBounds(buffer.Length, offset, size);
        }

        private static void ValidateBufferBounds(int bufferSize, int offset, int size)
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

        public static void ValidateDataIsEmptyOrJson(byte[] data)
        {
            if (data != null
                && data.Length != 0)
            {
                using var stream = new MemoryStream(data);
                using var streamReader = new StreamReader(stream, Encoding.UTF8, false, Math.Min(1024, data.Length));

                using var reader = new JsonTextReader(streamReader);

                while (reader.Read())
                {
                }
            }
        }

        public static IReadOnlyDictionary<TKey, TValue> MergeDictionaries<TKey, TValue>(IDictionary<TKey, TValue>[] dictionaries)
        {
            // No item in the array should be null.
            if (dictionaries == null || dictionaries.Any(item => item == null))
            {
                throw new ArgumentNullException(nameof(dictionaries), "Provided dictionaries should not be null");
            }

            var result = dictionaries.SelectMany(dict => dict)
                .ToLookup(pair => pair.Key, pair => pair.Value)
                .ToDictionary(group => group.Key, group => group.First());

            return new ReadOnlyDictionary<TKey, TValue>(result);
        }
    }
}
