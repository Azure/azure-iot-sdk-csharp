// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Encoding;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Common.Api;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTMessageConverter
    {
        private const string LockTokenName = "x-opt-lock-token";
        private const string SequenceNumberName = "x-opt-sequence-number";
        private const string TimeSpanName = AmqpConstants.Vendor + ":timespan";
        private const string UriName = AmqpConstants.Vendor + ":uri";
        private const string DateTimeOffsetName = AmqpConstants.Vendor + ":datetime-offset";
        private const string InputName = "x-opt-input-name";

        private const string AmqpDiagIdKey = "Diagnostic-Id";
        private const string AmqpDiagCorrelationContextKey = "Correlation-Context";

        private const string MethodName = "IoThub-methodname";
        private const string Status = "IoThub-status";

        #region AmqpMessage <--> Message

        public static Message AmqpMessageToMessage(AmqpMessage amqpMessage)
        {
            if (amqpMessage == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(AmqpMessage));
            }
            Stream stream = amqpMessage.BodyStream;
            Message message = new Message(stream, true);
            UpdateMessageHeaderAndProperties(amqpMessage, message);
            return message;
        }

        public static AmqpMessage MessageToAmqpMessage(Message message, bool setBodyCalled = true)
        {
            if (message == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(Message));
            }
            message.ThrowIfDisposed();

            AmqpMessage amqpMessage = null;

            if (message.HasBodyStream())
            {
                amqpMessage = AmqpMessage.Create(message.GetBodyStream(), false);
            }
            else
            {
                amqpMessage = AmqpMessage.Create();
            }
            UpdateAmqpMessageHeadersAndProperties(amqpMessage, message);

            return amqpMessage;
        }

        /// <summary>
        /// Copies the properties from the amqp message to the Message instance.
        /// </summary>
        public static void UpdateMessageHeaderAndProperties(AmqpMessage amqpMessage, Message message)
        {
            Fx.AssertAndThrow(amqpMessage.DeliveryTag != null, "AmqpMessage should always contain delivery tag.");
            message.DeliveryTag = amqpMessage.DeliveryTag;

            SectionFlag sections = amqpMessage.Sections;
            if ((sections & SectionFlag.Properties) != 0)
            {
                // Extract only the Properties that we support
                message.MessageId = amqpMessage.Properties.MessageId != null ? amqpMessage.Properties.MessageId.ToString() : null;
                message.To = amqpMessage.Properties.To != null ? amqpMessage.Properties.To.ToString() : null;

                if (amqpMessage.Properties.AbsoluteExpiryTime.HasValue)
                {
                    message.ExpiryTimeUtc = amqpMessage.Properties.AbsoluteExpiryTime.Value;
                }

                message.CorrelationId = amqpMessage.Properties.CorrelationId != null ? amqpMessage.Properties.CorrelationId.ToString() : null;

                if (!string.IsNullOrWhiteSpace(amqpMessage.Properties.ContentType.Value))
                {
                    message.ContentType = amqpMessage.Properties.ContentType.Value;
                }

                if (!string.IsNullOrWhiteSpace(amqpMessage.Properties.ContentEncoding.Value))
                {
                    message.ContentEncoding = amqpMessage.Properties.ContentEncoding.Value;
                }

                message.UserId = amqpMessage.Properties.UserId.Array != null ? Encoding.UTF8.GetString(amqpMessage.Properties.UserId.Array, 0 /*index*/, amqpMessage.Properties.UserId.Array.Length) : null;
            }

            if ((sections & SectionFlag.MessageAnnotations) != 0)
            {
                if (amqpMessage.MessageAnnotations.Map.TryGetValue(LockTokenName, out string lockToken))
                {
                    message.LockToken = lockToken;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(SequenceNumberName, out ulong sequenceNumber))
                {
                    message.SequenceNumber = sequenceNumber;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.EnqueuedTime, out DateTime enqueuedTime))
                {
                    message.EnqueuedTimeUtc = enqueuedTime;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.DeliveryCount, out byte deliveryCount))
                {
                    message.DeliveryCount = deliveryCount;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(InputName, out string inputName))
                {
                    message.InputName = inputName;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.ConnectionDeviceId, out string connectionDeviceId))
                {
                    message.ConnectionDeviceId = connectionDeviceId;
                }

                if (amqpMessage.MessageAnnotations.Map.TryGetValue(MessageSystemPropertyNames.ConnectionModuleId, out string connectionModuleId))
                {
                    message.ConnectionModuleId = connectionModuleId;
                }
            }

            if ((sections & SectionFlag.ApplicationProperties) != 0)
            {
                foreach (KeyValuePair<MapKey, object> pair in amqpMessage.ApplicationProperties.Map)
                {
                    object netObject = null;
                    if (TryGetNetObjectFromAmqpObject(pair.Value, MappingType.ApplicationProperty, out netObject))
                    {
                        var stringObject = netObject as string;

                        if (stringObject != null)
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
                                    message.CreationTimeUtc = DateTime.Parse(stringObject);
                                    break;

                                default:
                                    message.Properties[pair.Key.ToString()] = stringObject;
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

            if (!data.ExpiryTimeUtc.Equals(default(DateTime)))
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

            object propertyValue;
            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.Ack, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map["iothub-ack"] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.MessageSchema, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.MessageSchema] = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.CreationTimeUtc, out propertyValue))
            {
                amqpMessage.ApplicationProperties.Map[MessageSystemPropertyNames.CreationTimeUtc] = ((DateTime)propertyValue).ToString("o");    // Convert to string that complies with ISO 8601
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentType, out propertyValue))
            {
                amqpMessage.Properties.ContentType = (string)propertyValue;
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentEncoding, out propertyValue))
            {
                amqpMessage.Properties.ContentEncoding = (string)propertyValue;
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
                foreach (var pair in data.Properties)
                {
                    object amqpObject;
                    if (TryGetAmqpObjectFromNetObject(pair.Value, MappingType.ApplicationProperty, out amqpObject))
                    {
                        amqpMessage.ApplicationProperties.Map[pair.Key] = amqpObject;
                    }
                }
            }

            if (IoTHubClientDiagnostic.HasDiagnosticProperties(data))
            {
                amqpMessage.MessageAnnotations.Map[AmqpDiagIdKey] = data.SystemProperties[MessageSystemPropertyNames.DiagId];
                amqpMessage.MessageAnnotations.Map[AmqpDiagCorrelationContextKey] = data.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext];
            }
        }

        #endregion AmqpMessage <--> Message

        #region AmqpMessage <--> Methods

        public static AmqpMessage ConvertMethodResponseInternalToAmqpMessage(MethodResponseInternal methodResponseInternal, bool setBodyCalled = true)
        {
            methodResponseInternal.ThrowIfDisposed();

            AmqpMessage amqpMessage = null;

            if (methodResponseInternal.BodyStream == null)
            {
                amqpMessage = AmqpMessage.Create();
            }
            else
            {
                amqpMessage = AmqpMessage.Create(methodResponseInternal.BodyStream, false);
            }
            PopulateAmqpMessageFromMethodResponse(amqpMessage, methodResponseInternal);
            return amqpMessage;
        }

        /// <summary>
        /// Copies the properties from the amqp message to the MethodRequest instance.
        /// </summary>
        public static MethodRequestInternal ConstructMethodRequestFromAmqpMessage(AmqpMessage amqpMessage, CancellationToken cancellationToken)
        {
            if (amqpMessage == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(AmqpMessage));
            }

            string methodRequestId = string.Empty;
            string methodName = string.Empty;

            SectionFlag sections = amqpMessage.Sections;
            if ((sections & SectionFlag.Properties) != 0)
            {
                // Extract only the Properties that we support
                methodRequestId = amqpMessage.Properties.CorrelationId != null ? amqpMessage.Properties.CorrelationId.ToString() : null;
            }

            if ((sections & SectionFlag.ApplicationProperties) != 0)
            {
                if (!(amqpMessage.ApplicationProperties?.Map.TryGetValue(new MapKey(MethodName), out methodName) ?? false))
                {
                    Fx.Exception.TraceHandled(new InvalidDataException("Method name is missing"), "MethodConverter.ConstructMethodRequestFromAmqpMessage");
                }
            }

            return new MethodRequestInternal(methodName, methodRequestId, amqpMessage.BodyStream, cancellationToken);
        }

        /// <summary>
        /// Copies the Method instance's properties to the AmqpMessage instance.
        /// </summary>
        public static void PopulateAmqpMessageFromMethodResponse(AmqpMessage amqpMessage, MethodResponseInternal methodResponseInternal)
        {
            Fx.Assert(methodResponseInternal.RequestId != null, "Request Id is missing in the methodResponse.");

            amqpMessage.Properties.CorrelationId = new Guid(methodResponseInternal.RequestId);

            if (amqpMessage.ApplicationProperties == null)
            {
                amqpMessage.ApplicationProperties = new ApplicationProperties();
            }

            amqpMessage.ApplicationProperties.Map[Status] = methodResponseInternal.Status;
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
                    if (amqpObject is AmqpSymbol)
                    {
                        netObject = ((AmqpSymbol)amqpObject).Value;
                    }
                    else if (amqpObject is ArraySegment<byte>)
                    {
                        ArraySegment<byte> binValue = (ArraySegment<byte>)amqpObject;
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
                    else if (amqpObject is DescribedType)
                    {
                        DescribedType describedType = (DescribedType)amqpObject;
                        if (describedType.Descriptor is AmqpSymbol)
                        {
                            AmqpSymbol symbol = (AmqpSymbol)describedType.Descriptor;
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
                    else if (amqpObject is AmqpMap)
                    {
                        AmqpMap map = (AmqpMap)amqpObject;
                        Dictionary<string, object> dictionary = new Dictionary<string, object>();
                        foreach (var pair in map)
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
                    if (netObject is Stream)
                    {
                        if (mappingType == MappingType.ApplicationProperty)
                        {
                            amqpObject = ReadStream((Stream)netObject);
                        }
                    }
                    else if (mappingType == MappingType.ApplicationProperty)
                    {
                        throw FxTrace.Exception.AsError(new SerializationException(IotHubApiResources.GetString(ApiResources.FailedToSerializeUnsupportedType, netObject.GetType().FullName)));
                    }
                    else if (netObject is byte[])
                    {
                        amqpObject = new ArraySegment<byte>((byte[])netObject);
                    }
                    else if (netObject is IList)
                    {
                        // Array is also an IList
                        amqpObject = netObject;
                    }
                    else if (netObject is IDictionary)
                    {
                        amqpObject = new AmqpMap((IDictionary)netObject);
                    }
                    break;

                default:
                    break;
            }

            return amqpObject != null;
        }

        internal static ArraySegment<byte> ReadStream(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
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
