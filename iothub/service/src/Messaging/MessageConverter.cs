// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using System.Globalization;

namespace Microsoft.Azure.Devices
{
    internal static class MessageConverter
    {
        internal const string LockTokenName = "x-opt-lock-token";
        internal const string SequenceNumberName = "x-opt-sequence-number";
        internal const string TimeSpanName = AmqpConstants.Vendor + ":timespan";
        internal const string UriName = AmqpConstants.Vendor + ":uri";
        internal const string DateTimeOffsetName = AmqpConstants.Vendor + ":datetime-offset";

        /// <summary>
        /// Copies the message instance's properties to the AMQP message instance.
        /// </summary>
        internal static void UpdateAmqpMessageHeadersAndProperties(AmqpMessage amqpMessage, Message data, bool copyUserProperties = true)
        {
            amqpMessage.Properties.MessageId = data.MessageId;

            if (data.To != null)
            {
                amqpMessage.Properties.To = data.To;
            }

            if (!data.ExpiresOnUtc.Equals(default))
            {
                amqpMessage.Properties.AbsoluteExpiryTime = data.ExpiresOnUtc.UtcDateTime;
            }

            if (data.CorrelationId != null)
            {
                amqpMessage.Properties.CorrelationId = data.CorrelationId;
            }

            if (data.UserId != null)
            {
                amqpMessage.Properties.UserId = new ArraySegment<byte>(Encoding.UTF8.GetBytes(data.UserId));
            }

            if (amqpMessage.ApplicationProperties == null)
            {
                amqpMessage.ApplicationProperties = new ApplicationProperties();
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.Ack, out object propertyValue))
            {
                amqpMessage.ApplicationProperties.Map["iothub-ack"] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.MessageSchema, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.MessageSchema] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.CreationTimeUtc, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.CreationTimeUtc] = ((DateTime)propertyValue).ToString("o", CultureInfo.InvariantCulture);   // Convert to string that complies with ISO 8601
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentType, out propertyValue))
            {
                amqpMessage.Properties.ContentType = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentEncoding, out propertyValue))
            {
                amqpMessage.Properties.ContentEncoding = (string)propertyValue;
            }

            if (copyUserProperties && data.Properties.Count > 0)
            {
                foreach (KeyValuePair<string, string> pair in data.Properties)
                {
                    if (TryGetAmqpObjectFromNetObject(pair.Value, MappingType.ApplicationProperty, out object amqpObject))
                    {
                        amqpMessage.ApplicationProperties.Map[pair.Key] = amqpObject;
                    }
                }
            }
        }

        internal static bool TryGetAmqpObjectFromNetObject(object netObject, MappingType mappingType, out object amqpObject)
        {
            amqpObject = null;
            if (netObject == null)
            {
                return false;
            }

            switch (SerializationUtilities.GetTypeId(netObject))
            {
                case PropertyValueType.Byte:
                case PropertyValueType.SByte:
                case PropertyValueType.Int16:
                case PropertyValueType.Int32:
                case PropertyValueType.Int64:
                case PropertyValueType.UInt16:
                case PropertyValueType.UInt32:
                case PropertyValueType.UInt64:
                case PropertyValueType.Single:
                case PropertyValueType.Double:
                case PropertyValueType.Boolean:
                case PropertyValueType.Decimal:
                case PropertyValueType.Char:
                case PropertyValueType.Guid:
                case PropertyValueType.DateTime:
                case PropertyValueType.String:
                    amqpObject = netObject;
                    break;

                case PropertyValueType.Stream:
                    if (mappingType == MappingType.ApplicationProperty)
                    {
                        amqpObject = ReadStream((Stream)netObject);
                    }
                    break;

                case PropertyValueType.Uri:
                    amqpObject = new DescribedType((AmqpSymbol)UriName, ((Uri)netObject).AbsoluteUri);
                    break;

                case PropertyValueType.DateTimeOffset:
                    amqpObject = new DescribedType((AmqpSymbol)DateTimeOffsetName, ((DateTimeOffset)netObject).UtcTicks);
                    break;

                case PropertyValueType.TimeSpan:
                    amqpObject = new DescribedType((AmqpSymbol)TimeSpanName, ((TimeSpan)netObject).Ticks);
                    break;

                case PropertyValueType.Unknown:
                    if (netObject is Stream stream
                        && mappingType == MappingType.ApplicationProperty)
                    {
                        amqpObject = ReadStream(stream);
                    }
                    else if (mappingType == MappingType.ApplicationProperty)
                    {
                        throw new InvalidOperationException($"Serialization operation failed due to unsupported type '{netObject.GetType().FullName}'.");
                    }
                    else if (netObject is byte[] netObjectBytes)
                    {
                        amqpObject = new ArraySegment<byte>(netObjectBytes);
                    }
                    else if (netObject is IList)
                    {
                        // Array is also an IList
                        amqpObject = netObject;
                    }
                    else if (netObject is IDictionary dictionary)
                    {
                        amqpObject = new AmqpMap(dictionary);
                    }
                    break;

                default:
                    break;
            }

            return amqpObject != null;
        }

        internal static ArraySegment<byte> ReadStream(Stream stream)
        {
            using var memoryStream = new MemoryStream();
            int bytesRead;
            byte[] readBuffer = new byte[512];
            while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                memoryStream.Write(readBuffer, 0, bytesRead);
            }

            return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
        }

        internal static AmqpMessage MessageToAmqpMessage(Message message)
        {
            Argument.AssertNotNull(message, nameof(message));

            AmqpMessage amqpMessage = message.HasPayload
                ? AmqpMessage.Create(new MemoryStream(message.Payload), true)
                : AmqpMessage.Create();

            UpdateAmqpMessageHeadersAndProperties(amqpMessage, message);
            return amqpMessage;
        }
    }
}
