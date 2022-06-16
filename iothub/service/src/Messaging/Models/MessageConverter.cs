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
        public const string LockTokenName = "x-opt-lock-token";
        public const string SequenceNumberName = "x-opt-sequence-number";
        public const string TimeSpanName = AmqpConstants.Vendor + ":timespan";
        public const string UriName = AmqpConstants.Vendor + ":uri";
        public const string DateTimeOffsetName = AmqpConstants.Vendor + ":datetime-offset";

        /// <summary>
        /// Copies the properties from the AMQP message to the Message instance.
        /// </summary>
        public static void UpdateMessageHeaderAndProperties(AmqpMessage amqpMessage, Message data)
        {
            Fx.AssertAndThrow(amqpMessage.DeliveryTag != null, "AmqpMessage should always contain delivery tag.");
            data.DeliveryTag = amqpMessage.DeliveryTag;

            SectionFlag sections = amqpMessage.Sections;
            if ((sections & SectionFlag.Properties) != 0)
            {
                // Extract only the Properties that we support
                data.MessageId = amqpMessage.Properties.MessageId?.ToString();
                data.To = amqpMessage.Properties.To?.ToString();

                if (amqpMessage.Properties.AbsoluteExpiryTime.HasValue)
                {
                    data.ExpiryTimeUtc = amqpMessage.Properties.AbsoluteExpiryTime.Value;
                }

                data.CorrelationId = amqpMessage.Properties.CorrelationId?.ToString();
                data.UserId = amqpMessage.Properties.UserId.Array != null ? Encoding.UTF8.GetString(amqpMessage.Properties.UserId.Array) : null;

                if (!string.IsNullOrWhiteSpace(amqpMessage.Properties.ContentType.Value))
                {
                    data.ContentType = amqpMessage.Properties.ContentType.Value;
                }

                if (!string.IsNullOrWhiteSpace(amqpMessage.Properties.ContentEncoding.Value))
                {
                    data.ContentEncoding = amqpMessage.Properties.ContentEncoding.Value;
                }
            }

            if ((sections & SectionFlag.MessageAnnotations) != 0)
            {
                if (amqpMessage.MessageAnnotations.Map.TryGetValue(LockTokenName, out string lockToken))
                {
                    data.LockToken = lockToken;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(SequenceNumberName, out ulong sequenceNumber))
                {
                    data.SequenceNumber = sequenceNumber;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.EnqueuedTime, out DateTime enqueuedTime))
                {
                    data.EnqueuedTimeUtc = enqueuedTime;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.DeliveryCount, out byte deliveryCount))
                {
                    data.DeliveryCount = deliveryCount;
                }
            }

            if ((sections & SectionFlag.ApplicationProperties) != 0)
            {
                foreach (KeyValuePair<MapKey, object> pair in amqpMessage.ApplicationProperties.Map)
                {
                    if (TryGetNetObjectFromAmqpObject(pair.Value, MappingType.ApplicationProperty, out object netObject))
                    {
                        if (netObject is string stringObject)
                        {
                            switch (pair.Key.ToString())
                            {
                                case MessageSystemPropertyNames.Operation:
                                    data.SystemProperties[pair.Key.ToString()] = stringObject;
                                    break;

                                case MessageSystemPropertyNames.MessageSchema:
                                    data.MessageSchema = stringObject;
                                    break;

                                case MessageSystemPropertyNames.CreationTimeUtc:
                                    data.CreationTimeUtc = DateTime.Parse(stringObject, CultureInfo.InvariantCulture);
                                    break;

                                default:
                                    data.Properties[pair.Key.ToString()] = stringObject;
                                    break;
                            }
                        }
                        else
                        {
                            // TODO: RDBug 4093369 Handling of non-string property values in Amqp messages
                            // Drop non-string properties and log an error
                            Fx.Exception.TraceHandled(new InvalidDataException("IotHub does not accept non-string Amqp properties"), "MessageConverter.UpdateMessageHeaderAndProperties");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies the Message instance's properties to the AmqpMessage instance.
        /// </summary>
        public static void UpdateAmqpMessageHeadersAndProperties(AmqpMessage amqpMessage, Message data, bool copyUserProperties = true)
        {
            amqpMessage.Properties.MessageId = data.MessageId;

            if (data.To != null)
            {
                amqpMessage.Properties.To = data.To;
            }

            if (!data.ExpiryTimeUtc.Equals(default))
            {
                amqpMessage.Properties.AbsoluteExpiryTime = data.ExpiryTimeUtc;
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

        public static bool TryGetAmqpObjectFromNetObject(object netObject, MappingType mappingType, out object amqpObject)
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
                    if (netObject is Stream stream)
                    {
                        if (mappingType == MappingType.ApplicationProperty)
                        {
                            amqpObject = ReadStream(stream);
                        }
                    }
                    else if (mappingType == MappingType.ApplicationProperty)
                    {
                        throw FxTrace.Exception.AsError(new SerializationException(IotHubApiResources.GetString(ApiResources.FailedToSerializeUnsupportedType, netObject.GetType().FullName)));
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

        public static bool TryGetNetObjectFromAmqpObject(object amqpObject, MappingType mappingType, out object netObject)
        {
            netObject = null;
            if (amqpObject == null)
            {
                return false;
            }

            switch (SerializationUtilities.GetTypeId(amqpObject))
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
                    netObject = amqpObject;
                    break;

                case PropertyValueType.Unknown:
                    if (amqpObject is AmqpSymbol)
                    {
                        netObject = ((AmqpSymbol)amqpObject).Value;
                    }
                    else if (amqpObject is ArraySegment<byte> binValue)
                    {
                        if (binValue.Count == binValue.Array.Length)
                        {
                            netObject = binValue.Array;
                        }
                        else
                        {
                            byte[] buffer = new byte[binValue.Count];
                            Buffer.BlockCopy(binValue.Array, binValue.Offset, buffer, 0, binValue.Count);
                            netObject = buffer;
                        }
                    }
                    else if (amqpObject is DescribedType describedType)
                    {
                        if (describedType.Descriptor is AmqpSymbol symbol)
                        {
                            if (symbol.Equals((AmqpSymbol)UriName))
                            {
                                netObject = new Uri((string)describedType.Value);
                            }
                            else if (symbol.Equals((AmqpSymbol)TimeSpanName))
                            {
                                netObject = new TimeSpan((long)describedType.Value);
                            }
                            else if (symbol.Equals((AmqpSymbol)DateTimeOffsetName))
                            {
                                netObject = new DateTimeOffset(new DateTime((long)describedType.Value, DateTimeKind.Utc));
                            }
                        }
                    }
                    else if (mappingType == MappingType.ApplicationProperty)
                    {
                        throw FxTrace.Exception.AsError(new SerializationException(IotHubApiResources.GetString(ApiResources.FailedToSerializeUnsupportedType, amqpObject.GetType().FullName)));
                    }
                    else if (amqpObject is AmqpMap map)
                    {
                        var dictionary = new Dictionary<string, object>();
                        foreach (KeyValuePair<MapKey, object> pair in map)
                        {
                            dictionary.Add(pair.Key.ToString(), pair.Value);
                        }

                        netObject = dictionary;
                    }
                    else
                    {
                        netObject = amqpObject;
                    }
                    break;

                default:
                    break;
            }

            return netObject != null;
        }

        public static ArraySegment<byte> ReadStream(Stream stream)
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
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(Message));
            }
            message.ThrowIfDisposed();

            AmqpMessage amqpMessage = message.HasBodyStream()
                ? AmqpMessage.Create(message.GetBodyStream(), false)
                : AmqpMessage.Create();
            UpdateAmqpMessageHeadersAndProperties(amqpMessage, message);
            return amqpMessage;
        }
    }
}
