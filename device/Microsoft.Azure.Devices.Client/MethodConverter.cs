// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Runtime.Serialization;
    using System.Text;

#if !PCL

    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Encoding;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Devices.Client.Common.Api;

    static class MethodConverter
    {
        public const string MethodName = "IoThub-methodname";
        public const string Status = "IoThub-status";

        /// <summary>
        /// Copies the properties from the amqp message to the MethodRequest instance.
        /// </summary>
        public static MethodRequest ConstructMethodRequestFromAmqpMessage(AmqpMessage amqpMessage)
        {
            if (amqpMessage == null)
            {
                throw Fx.Exception.ArgumentNull("amqpMessage");
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
                foreach (KeyValuePair<MapKey, object> pair in amqpMessage.ApplicationProperties.Map)
                {
                    object netObject = null;
                    if (TryGetNetObjectFromAmqpObject(pair.Value, MappingType.ApplicationProperty, out netObject))
                    {
                        var stringObject = netObject as string;

                        // Extract only the method name from application property which we support
                        if ((stringObject != null) && (pair.Key.ToString() == MethodName))
                        {
                            methodName = pair.Value.ToString();
                        }
                        else
                        {
                            // Drop non-string properties and log an error
                            Fx.Exception.TraceHandled(new InvalidDataException("IotHub does not accept non-string Amqp properties"), "MethodConverter.UpdateMethodHeaderAndProperties");
                        }
                    }
                }
            }

            return new MethodRequest(methodName, methodRequestId, amqpMessage.BodyStream);
        }

        /// <summary>
        /// Copies the Method instance's properties to the AmqpMessage instance.
        /// </summary>
        public static void PopulateAmqpMessageFromMethodResponse(AmqpMessage amqpMessage, MethodResponse methodResponse)
        {
            if (methodResponse.RequestId != null)
            {
                amqpMessage.Properties.CorrelationId = new Guid(methodResponse.RequestId);
            }

            if (amqpMessage.ApplicationProperties == null)
            {
                amqpMessage.ApplicationProperties = new ApplicationProperties();
            }

            amqpMessage.ApplicationProperties.Map[Status] = (int)methodResponse.Status;
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

        public static ArraySegment<byte> ReadStream(Stream stream)
        {
            MemoryStream memoryStream = new MemoryStream();
            int bytesRead;
            byte[] readBuffer = new byte[512];
            while ((bytesRead = stream.Read(readBuffer, 0, readBuffer.Length)) > 0)
            {
                memoryStream.Write(readBuffer, 0, bytesRead);
            }

#if WINDOWS_UWP
// UWP doesn't have GetBuffer. ToArray creates a copy -- make sure perf impact is acceptable
            return new ArraySegment<byte>(memoryStream.ToArray(), 0, (int)memoryStream.Length);
#else
            return new ArraySegment<byte>(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
#endif
        }
    }
#endif
}
