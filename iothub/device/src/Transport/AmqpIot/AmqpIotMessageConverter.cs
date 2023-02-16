// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal sealed class AmqpIotMessageConverter
    {
        private const string SequenceNumberName = "x-opt-sequence-number";
        private const string TimeSpanName = AmqpConstants.Vendor + ":timespan";
        private const string UriName = AmqpConstants.Vendor + ":uri";
        private const string DateTimeOffsetName = AmqpConstants.Vendor + ":datetime-offset";
        private const string InputName = "x-opt-input-name";
        private const string MethodName = "IoThub-methodname";
        private const string Status = "IoThub-status";
        private const string FailedToSerializeUnsupportedType = "Failed to serialize an unsupported type of '{0}'.";

        #region AmqpMessage <--> Message

        internal static IncomingMessage AmqpMessageToIncomingMessage(AmqpMessage amqpMessage, PayloadConvention payloadConvention)
        {
            Argument.AssertNotNull(amqpMessage, nameof(amqpMessage));

            using var ms = new MemoryStream();
            using (amqpMessage)
            {
                amqpMessage.BodyStream.CopyTo(ms);

                var message = new IncomingMessage(ms.ToArray())
                {
                    PayloadConvention = payloadConvention,
                };

                UpdateMessageHeaderAndProperties(amqpMessage, message);

                return message;
            }
        }

        internal static AmqpMessage OutgoingMessageToAmqpMessage(TelemetryMessage message)
        {
            Argument.AssertNotNull(message, nameof(message));

            AmqpMessage amqpMessage = message.Payload != null
                ? AmqpMessage.Create(new MemoryStream(message.GetPayloadObjectBytes()), true)
                : AmqpMessage.Create();

            UpdateAmqpMessageHeadersAndProperties(message, amqpMessage);

            return amqpMessage;
        }

        /// <summary>
        /// Copies the properties from the AMQP message to the Message instance.
        /// </summary>
        internal static void UpdateMessageHeaderAndProperties(AmqpMessage amqpMessage, IncomingMessage message)
        {
            if (amqpMessage.DeliveryTag == null)
            {
                throw new InvalidOperationException("AmqpMessage should always contain delivery tag.");
            }

            SectionFlag sections = amqpMessage.Sections;
            if ((sections & SectionFlag.Properties) != 0)
            {
                // Extract only the Properties that we support
                message.MessageId = amqpMessage.Properties.MessageId?.ToString();
                message.To = amqpMessage.Properties.To?.ToString();

                if (amqpMessage.Properties.AbsoluteExpiryTime.HasValue)
                {
                    message.ExpiresOnUtc = amqpMessage.Properties.AbsoluteExpiryTime.Value;
                }

                message.CorrelationId = amqpMessage.Properties.CorrelationId?.ToString();

                if (!string.IsNullOrWhiteSpace(amqpMessage.Properties.ContentType.Value))
                {
                    message.ContentType = amqpMessage.Properties.ContentType.Value;
                }

                if (!string.IsNullOrWhiteSpace(amqpMessage.Properties.ContentEncoding.Value))
                {
                    message.ContentEncoding = amqpMessage.Properties.ContentEncoding.Value;
                }

                message.UserId = amqpMessage.Properties.UserId.Array == null
                    ? null
                    : Encoding.UTF8.GetString(amqpMessage.Properties.UserId.Array, 0, amqpMessage.Properties.UserId.Array.Length);
            }

            if ((sections & SectionFlag.MessageAnnotations) != 0)
            {
                if (amqpMessage.MessageAnnotations.Map.TryGetValue(SequenceNumberName, out ulong sequenceNumber))
                {
                    message.SequenceNumber = sequenceNumber;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.EnqueuedTime, out DateTime enqueuedTime))
                {
                    message.EnqueuedOnUtc = enqueuedTime;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(InputName, out string inputName))
                {
                    message.InputName = inputName;
                }
            }

            if ((sections & SectionFlag.ApplicationProperties) != 0)
            {
                foreach (KeyValuePair<MapKey, object> pair in amqpMessage.ApplicationProperties.Map)
                {
                    if (TryGetNetObjectFromAmqpObject(pair.Value, MappingType.ApplicationProperty, out object netObject)
                        && netObject is string stringObject)
                    {
                        switch (pair.Key.ToString())
                        {
                            case MessageSystemPropertyNames.Operation:
                                message.SystemProperties[pair.Key.ToString()] = stringObject;
                                break;

                            case MessageSystemPropertyNames.MessageSchema:
                                message.MessageSchema = stringObject;
                                break;

                            case MessageSystemPropertyNames.CreationTimeUtc:
                                message.CreatedOnUtc = DateTime.Parse(stringObject, CultureInfo.InvariantCulture);
                                break;

                            default:
                                message.Properties[pair.Key.ToString()] = stringObject;
                                break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Copies the Message instance's properties to the AmqpMessage instance.
        /// </summary>
        internal static void UpdateAmqpMessageHeadersAndProperties(TelemetryMessage data, AmqpMessage amqpMessage, bool copyUserProperties = true)
        {
            // First populate the required fields defined in AmqpMessage.Properties

            amqpMessage.Properties.MessageId = data.MessageId;

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

            // Then populate the optional fields defined in AmqpMessage.Properties

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentType, out object propertyValue))
            {
                amqpMessage.Properties.ContentType = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentEncoding, out propertyValue))
            {
                amqpMessage.Properties.ContentEncoding = (string)propertyValue;
            }

            // Now populate the additional TelemetryMessage SystemProperties into the map AmqpMessage.ApplicationProperties

            amqpMessage.ApplicationProperties ??= new ApplicationProperties();

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.MessageSchema, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.MessageSchema] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.CreationTimeUtc, out propertyValue))
            {
                // Convert to string that complies with ISO 8601
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.CreationTimeUtc] = ((DateTimeOffset)propertyValue).ToString("o", CultureInfo.InvariantCulture);
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.OutputName, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.OutputName] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.InterfaceId, out propertyValue))
            {
                amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.InterfaceId] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ComponentName, out propertyValue))
            {
                amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.ComponentName] = (string)propertyValue;
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

        #endregion AmqpMessage <--> Message

        #region AmqpMessage <--> Methods

        internal static AmqpMessage ConvertDirectMethodResponseToAmqpMessage(DirectMethodResponse directMethodResponse)
        {
            AmqpMessage amqpMessage = directMethodResponse.Payload == null
                ? AmqpMessage.Create()
                : AmqpMessage.Create(new MemoryStream(directMethodResponse.GetPayloadObjectBytes()), true);

            PopulateAmqpMessageFromMethodResponse(amqpMessage, directMethodResponse);
            return amqpMessage;
        }

        /// <summary>
        /// Copies the properties from the AMQP message to the MethodRequest instance.
        /// </summary>
        internal static DirectMethodRequest ConstructMethodRequestFromAmqpMessage(AmqpMessage amqpMessage, PayloadConvention payloadConvention)
        {
            Argument.AssertNotNull(amqpMessage, nameof(amqpMessage));

            string methodRequestId = string.Empty;
            string methodName = string.Empty;

            using (amqpMessage)
            {
                SectionFlag sections = amqpMessage.Sections;
                if ((sections & SectionFlag.Properties) != 0)
                {
                    // Extract only the Properties that we support
                    methodRequestId = amqpMessage.Properties.CorrelationId?.ToString();
                }

                amqpMessage.ApplicationProperties?.Map.TryGetValue(new MapKey(MethodName), out methodName);

                using var ms = new MemoryStream();
                amqpMessage.BodyStream.CopyTo(ms);
                return new DirectMethodRequest(methodName)
                {
                    PayloadConvention = payloadConvention,
                    Payload = ms.ToArray(),
                    RequestId = methodRequestId,
                };
            }
        }

        /// <summary>
        /// Copies the Method instance's properties to the AmqpMessage instance.
        /// </summary>
        internal static void PopulateAmqpMessageFromMethodResponse(AmqpMessage amqpMessage, DirectMethodResponse directMethodResponse)
        {
            Debug.Assert(directMethodResponse.RequestId != null, "Request Id is missing in the methodResponse.");

            amqpMessage.Properties.CorrelationId = new Guid(directMethodResponse.RequestId);

            amqpMessage.ApplicationProperties ??= new ApplicationProperties();

            amqpMessage.ApplicationProperties.Map[Status] = directMethodResponse.Status;
        }

        #endregion AmqpMessage <--> Methods

        private static bool TryGetNetObjectFromAmqpObject(object amqpObject, MappingType mappingType, out object netObject)
        {
            netObject = null;
            if (amqpObject == null)
            {
                return true;
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
                    if (amqpObject is AmqpSymbol amqpSymbol)
                    {
                        netObject = amqpSymbol.Value;
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
                        if (describedType.Descriptor is AmqpSymbol descriptorAmqpSymbol)
                        {
                            if (descriptorAmqpSymbol.Equals(UriName))
                            {
                                netObject = new Uri((string)describedType.Value);
                            }
                            else if (descriptorAmqpSymbol.Equals(TimeSpanName))
                            {
                                netObject = new TimeSpan((long)describedType.Value);
                            }
                            else if (descriptorAmqpSymbol.Equals(DateTimeOffsetName))
                            {
                                netObject = new DateTimeOffset(new DateTime((long)describedType.Value, DateTimeKind.Utc));
                            }
                        }
                    }
                    else if (mappingType == MappingType.ApplicationProperty)
                    {
                        throw new SerializationException(
                            string.Format(CultureInfo.InvariantCulture, FailedToSerializeUnsupportedType, amqpObject.GetType().FullName));
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
            }

            return netObject != null;
        }

        private static bool TryGetAmqpObjectFromNetObject(object netObject, MappingType mappingType, out object amqpObject)
        {
            amqpObject = null;
            if (netObject == null)
            {
                return true;
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
                    if (netObject is Stream netStream)
                    {
                        if (mappingType == MappingType.ApplicationProperty)
                        {
                            amqpObject = ReadStream(netStream);
                        }
                    }
                    else if (mappingType == MappingType.ApplicationProperty)
                    {
                        throw new SerializationException(
                            string.Format(CultureInfo.InvariantCulture, FailedToSerializeUnsupportedType, netObject.GetType().FullName));
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
                    else if (netObject is IDictionary netObjectDictionary)
                    {
                        amqpObject = new AmqpMap(netObjectDictionary);
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
    }
}
