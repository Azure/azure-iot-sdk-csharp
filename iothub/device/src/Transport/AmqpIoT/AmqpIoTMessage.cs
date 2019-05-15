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
    internal class AmqpIoTMessage : IDisposable
    {
        public const string LockTokenName = "x-opt-lock-token";
        public const string SequenceNumberName = "x-opt-sequence-number";
        public const string TimeSpanName = AmqpIoTConstants.Vendor + ":timespan";
        public const string UriName = AmqpIoTConstants.Vendor + ":uri";
        public const string DateTimeOffsetName = AmqpIoTConstants.Vendor + ":datetime-offset";
        public const string InputName = "x-opt-input-name";

        private const string AmqpDiagIdKey = "Diagnostic-Id";
        private const string AmqpDiagCorrelationContextKey = "Correlation-Context";

        private AmqpMessage _amqpMessage;
        private Data data;
        private const string MethodNameKey = "IoThub-methodname";
        private const string MethodStatusKey = "IoThub-status";


        public AmqpIoTMessage()
        {
            _amqpMessage = AmqpMessage.Create();
        }

        public AmqpIoTMessage(AmqpMessage amqpMessage)
        {
            _amqpMessage = amqpMessage;
        }

        public AmqpIoTMessage(Stream stream, bool ownStream)
        {
            _amqpMessage = AmqpMessage.Create(stream, ownStream);
        }

        public AmqpIoTMessage(IEnumerable<Data> dataList)
        {
            _amqpMessage = AmqpMessage.Create(dataList);
        }

        public AmqpIoTMessage(Data data)
        {
            _amqpMessage = AmqpMessage.Create(data);
        }

        internal AmqpMessage GetMessage()
        {
            return _amqpMessage;
        }

        public void Dispose()
        {
            _amqpMessage.Dispose();
        }

        public virtual Stream BodyStream
        {
            get { return _amqpMessage.BodyStream; }
            set { _amqpMessage.BodyStream = value; }
        }

        internal ArraySegment<byte> GetDeliveryTag()
        {
            return _amqpMessage.DeliveryTag;
        }

        internal bool HasProperties()
        {
            return ((_amqpMessage.Sections & SectionFlag.Properties) != 0);
        }

        internal string GetMessageId()
        {
            return _amqpMessage.Properties?.MessageId != null ? _amqpMessage.Properties.MessageId.ToString() : null;
        }

        internal void SetMessageId(string messageId)
        {
            _amqpMessage.Properties.MessageId = messageId;
        }

        internal void SetMessageFormat(uint amqpConstants)
        {
            _amqpMessage.MessageFormat = amqpConstants;
        }

        internal string GetTo()
        {
            return _amqpMessage.Properties?.To != null ? _amqpMessage.Properties.To.ToString() : null;
        }

        internal void SetTo(string to)
        {
            _amqpMessage.Properties.To = to;
        }

        internal bool ExpiryTimeHasValue()
        {
            return _amqpMessage.Properties.AbsoluteExpiryTime.HasValue;
        }

        internal void SetAbsoluteExpiryTime(DateTime expiryTimeUtc)
        {
            _amqpMessage.Properties.AbsoluteExpiryTime = expiryTimeUtc;
        }

        internal DateTime GetExpiryTime()
        {
            return _amqpMessage.Properties.AbsoluteExpiryTime.Value;
        }

        internal string GetCorrelationId()
        {
            return _amqpMessage.Properties.CorrelationId != null ? _amqpMessage.Properties.CorrelationId.ToString() : null;
        }

        internal void SetCorrelationId(string correlationId)
        {
            _amqpMessage.Properties.CorrelationId = correlationId;
        }

        internal void SetUserId(ArraySegment<byte> userId)
        {
            _amqpMessage.Properties.UserId = userId;
        }

        internal bool ContentTypeHasValue()
        {
            return (!string.IsNullOrWhiteSpace(_amqpMessage.Properties.ContentType.Value));
        }

        internal string GetContentType()
        {
            return _amqpMessage.Properties.ContentType.Value;
        }

        internal void SetContentType(string value)
        {
            _amqpMessage.Properties.ContentType = value;
        }

        internal bool ContentEncodingHasValue()
        {
            return (!string.IsNullOrWhiteSpace(_amqpMessage.Properties.ContentEncoding.Value));
        }

        internal string GetContentEncoding()
        {
            return _amqpMessage.Properties.ContentEncoding.Value;
        }

        internal void SetContentEncoding(string value)
        {
            _amqpMessage.Properties.ContentEncoding = value;
        }

        internal bool HasMessageAnnotations()
        {
            return ((_amqpMessage.Sections & SectionFlag.MessageAnnotations) != 0);
        }

        internal bool MessageAnnotationsHasValue(string name, out string value)
        {
            return (_amqpMessage.MessageAnnotations.Map.TryGetValue(name, out value));
        }

        internal bool MessageAnnotationsHasValue(string name, out byte value)
        {
            return (_amqpMessage.MessageAnnotations.Map.TryGetValue(name, out value));
        }

        internal bool MessageAnnotationsHasValue(string name, out int value)
        {
            return (_amqpMessage.MessageAnnotations.Map.TryGetValue(name, out value));
        }

        internal bool MessageAnnotationsHasValue(string name, out ulong value)
        {
            return (_amqpMessage.MessageAnnotations.Map.TryGetValue(name, out value));
        }

        internal bool MessageAnnotationsHasValue(string name, out DateTime value)
        {
            return (_amqpMessage.MessageAnnotations.Map.TryGetValue(name, out value));
        }

        internal object GetMessageAnnotation(string key)
        {
            return _amqpMessage.MessageAnnotations.Map[key];
        }

        internal void SetMessageAnnotations(string key, object value)
        {
            _amqpMessage.MessageAnnotations.Map[key] = value;
        }

        internal bool HasApplicationProperties()
        {
            return ((_amqpMessage.Sections & SectionFlag.ApplicationProperties) != 0);
        }

        internal void CreateApplicationProperties()
        {
            if (_amqpMessage.ApplicationProperties == null)
            {
                _amqpMessage.ApplicationProperties = new ApplicationProperties();
            }
        }

        internal void SetApplicationProperty(string key, string value)
        {
            _amqpMessage.ApplicationProperties.Map[key] = value;
        }

        internal void GetApplicationProperties(Message data, string uriName, string dateTimeOffsetName, string timeSpanName)
        {
            foreach (KeyValuePair<MapKey, object> pair in _amqpMessage.ApplicationProperties.Map)
            {
                object netObject = null;
                if (TryGetNetObjectFromAmqpObject(pair.Value, MappingType.ApplicationProperty, uriName, dateTimeOffsetName, timeSpanName, out netObject))
                {
                    var stringObject = netObject as string;

                    if (stringObject != null)
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
                                data.CreationTimeUtc = DateTime.Parse(stringObject);
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

        internal void SetApplicationProperties(Message data, string uriName, string dateTimeOffsetName, string timeSpanName)
        {
            foreach (var pair in data.Properties)
            {
                object amqpObject;
                if (TryGetAmqpObjectFromNetObject(pair.Value, MappingType.ApplicationProperty, uriName, dateTimeOffsetName, timeSpanName, out amqpObject))
                {
                    _amqpMessage.ApplicationProperties.Map[pair.Key] = amqpObject;
                }
            }
        }

        internal Stream ToStream()
        {
            return _amqpMessage.ToStream();
        }

        internal MethodRequestInternal ConstructMethodRequestFromAmqpIoTMessage(CancellationToken cancellationToken)
        {
            string methodRequestId = string.Empty;
            string methodName = string.Empty;

            SectionFlag sections = _amqpMessage.Sections;
            if ((sections & SectionFlag.Properties) != 0)
            {
                // Extract only the Properties that we support
                methodRequestId = _amqpMessage.Properties.CorrelationId != null ? _amqpMessage.Properties.CorrelationId.ToString() : null;
            }

            if ((sections & SectionFlag.ApplicationProperties) != 0)
            {
                if (!(_amqpMessage.ApplicationProperties?.Map.TryGetValue(new MapKey(MethodNameKey), out methodName) ?? false))
                {
                    Fx.Exception.TraceHandled(new InvalidDataException("Method name is missing"), "MethodConverter.ConstructMethodRequestFromAmqpIoTMessage");
                }
            }

            return new MethodRequestInternal(methodName, methodRequestId, _amqpMessage.BodyStream, cancellationToken);
        }

        internal void PopulateAmqpIoTMessageFromMethodResponse(MethodResponseInternal methodResponseInternal)
        {
            Fx.Assert(methodResponseInternal.RequestId != null, "Request Id is missing in the methodResponse.");

            _amqpMessage.Properties.CorrelationId = new Guid(methodResponseInternal.RequestId);

            if (_amqpMessage.ApplicationProperties == null)
            {
                _amqpMessage.ApplicationProperties = new ApplicationProperties();
            }

            _amqpMessage.ApplicationProperties.Map[MethodStatusKey] = methodResponseInternal.Status;
        }

        private bool TryGetAmqpObjectFromNetObject(object netObject, MappingType mappingType, string uriName, string dateTimeOffsetName, string timeSpanName, out object amqpObject)
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
                    amqpObject = new DescribedType((AmqpSymbol)uriName, ((Uri)netObject).AbsoluteUri);
                    break;
                case PropertyValueType.DateTimeOffset:
                    amqpObject = new DescribedType((AmqpSymbol)dateTimeOffsetName, ((DateTimeOffset)netObject).UtcTicks);
                    break;
                case PropertyValueType.TimeSpan:
                    amqpObject = new DescribedType((AmqpSymbol)timeSpanName, ((TimeSpan)netObject).Ticks);
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

        private bool TryGetNetObjectFromAmqpObject(object amqpObject, MappingType mappingType, string uriName, string dateTimeOffsetName, string timeSpanName, out object netObject)
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
                            if (symbol.Equals((AmqpSymbol)uriName))
                            {
                                netObject = new Uri((string)describedType.Value);
                            }
                            else if (symbol.Equals((AmqpSymbol)timeSpanName))
                            {
                                netObject = new TimeSpan((long)describedType.Value);
                            }
                            else if (symbol.Equals((AmqpSymbol)dateTimeOffsetName))
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

        internal ArraySegment<byte> ReadStream(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            int bytesRead;
            byte[] readBuffer = new byte[512];
            while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                memoryStream.Write(readBuffer, 0, bytesRead);
            }

#if NETSTANDARD1_3
                    // UWP doesn't have GetBuffer. ToArray creates a copy -- make sure perf impact is acceptable
                    return new ArraySegment<byte>(memoryStream.ToArray(), 0, (int)memoryStream.Length);
#else
            return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
#endif
        }

        /// <summary>
        /// Copies the properties from the amqp message to the Message instance.
        /// </summary>
        internal static void UpdateMessageHeaderAndProperties(AmqpIoTMessage amqpIoTMessage, Message data)
        {
            Fx.AssertAndThrow(amqpIoTMessage.GetDeliveryTag() != null, "AmqpMessage should always contain delivery tag.");
            data.DeliveryTag = amqpIoTMessage.GetDeliveryTag();

            if (amqpIoTMessage.HasProperties())
            {
                // Extract only the Properties that we support
                data.MessageId = amqpIoTMessage.GetMessageId();
                data.To = amqpIoTMessage.GetTo();

                if (amqpIoTMessage.ExpiryTimeHasValue())
                {
                    data.ExpiryTimeUtc = amqpIoTMessage.GetExpiryTime();
                }

                data.CorrelationId = amqpIoTMessage.GetCorrelationId();

                if (amqpIoTMessage.ContentTypeHasValue())
                {
                    data.ContentType = amqpIoTMessage.GetContentType();
                }

                if (amqpIoTMessage.ContentEncodingHasValue())
                {
                    data.ContentEncoding = amqpIoTMessage.GetContentEncoding();
                }
            }

            if (amqpIoTMessage.HasMessageAnnotations())
            {
                if (amqpIoTMessage.MessageAnnotationsHasValue(LockTokenName, out string lockToken))
                {
                    data.LockToken = lockToken;
                }

                if (amqpIoTMessage.MessageAnnotationsHasValue(SequenceNumberName, out ulong sequenceNumber))
                {
                    data.SequenceNumber = sequenceNumber;
                }

                if (amqpIoTMessage.MessageAnnotationsHasValue(MessageSystemPropertyNames.EnqueuedTime, out DateTime enqueuedTime))
                {
                    data.EnqueuedTimeUtc = enqueuedTime;
                }

                if (amqpIoTMessage.MessageAnnotationsHasValue(MessageSystemPropertyNames.DeliveryCount, out byte deliveryCount))
                {
                    data.DeliveryCount = deliveryCount;
                }

                if (amqpIoTMessage.MessageAnnotationsHasValue(InputName, out string inputName))
                {
                    data.InputName = inputName;
                }

                if (amqpIoTMessage.MessageAnnotationsHasValue(MessageSystemPropertyNames.ConnectionDeviceId, out string connectionDeviceId))
                {
                    data.ConnectionDeviceId = connectionDeviceId;
                }

                if (amqpIoTMessage.MessageAnnotationsHasValue(MessageSystemPropertyNames.ConnectionModuleId, out string connectionModuleId))
                {
                    data.ConnectionModuleId = connectionModuleId;
                }
            }

            if (amqpIoTMessage.HasApplicationProperties())
            {
                amqpIoTMessage.GetApplicationProperties(data, UriName, DateTimeOffsetName, TimeSpanName);
            }
        }

        /// <summary>
        /// Copies the Message instance's properties to the AmqpMessage instance.
        /// </summary>
        internal static void UpdateAmqpMessageHeadersAndProperties(AmqpIoTMessage amqpIoTMessage, Message data, bool copyUserProperties = true)
        {
            amqpIoTMessage.SetMessageId(data.MessageId);
            if (data.To != null)
            {
                amqpIoTMessage.SetTo(data.To);
            }

            if (!data.ExpiryTimeUtc.Equals(default(DateTime)))
            {
                amqpIoTMessage.SetAbsoluteExpiryTime(data.ExpiryTimeUtc);
            }

            if (data.CorrelationId != null)
            {
                amqpIoTMessage.SetCorrelationId(data.CorrelationId);
            }

            if (data.UserId != null)
            {
                amqpIoTMessage.SetUserId(new ArraySegment<byte>(Encoding.UTF8.GetBytes(data.UserId)));
            }

            amqpIoTMessage.CreateApplicationProperties();

            object propertyValue;
            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentType, out propertyValue))
            {
                amqpIoTMessage.SetContentType((string)propertyValue);
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.ContentEncoding, out propertyValue))
            {
                amqpIoTMessage.SetContentEncoding((string)propertyValue);
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.Ack, out propertyValue))
            {
                amqpIoTMessage.SetApplicationProperty("iothub-ack", (string)propertyValue);
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.MessageSchema, out propertyValue))
            {
                amqpIoTMessage.SetApplicationProperty(MessageSystemPropertyNames.MessageSchema, (string)propertyValue);
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.CreationTimeUtc, out propertyValue))
            {
                // Convert to string that complies with ISO 8601
                amqpIoTMessage.SetApplicationProperty(MessageSystemPropertyNames.CreationTimeUtc, ((DateTime)propertyValue).ToString("o", System.Globalization.CultureInfo.InvariantCulture));
            }

            if (data.SystemProperties.TryGetValue(MessageSystemPropertyNames.OutputName, out propertyValue))
            {
                amqpIoTMessage.SetApplicationProperty(MessageSystemPropertyNames.OutputName, (string)propertyValue);
            }

            if (copyUserProperties && data.Properties.Count > 0)
            {
                amqpIoTMessage.SetApplicationProperties(data, UriName, DateTimeOffsetName, TimeSpanName);
            }

            if (IoTHubClientDiagnostic.HasDiagnosticProperties(data))
            {
                amqpIoTMessage.SetMessageAnnotations(AmqpDiagIdKey, data.SystemProperties[MessageSystemPropertyNames.DiagId]);
                amqpIoTMessage.SetMessageAnnotations(AmqpDiagCorrelationContextKey, data.SystemProperties[MessageSystemPropertyNames.DiagCorrelationContext]);
            }
        }
    }
}
