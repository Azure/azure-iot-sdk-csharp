// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.IO;
    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Encoding;
    using Microsoft.Azure.Amqp.Framing;

    static class MethodConverter
    {
        public const string MethodName = "IoThub-methodname";
        public const string Status = "IoThub-status";

        /// <summary>
        /// Copies the properties from the amqp message to the MethodRequest instance.
        /// </summary>
        public static MethodRequestInternal ConstructMethodRequestFromAmqpMessage(AmqpMessage amqpMessage)
        {
            if (amqpMessage == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(amqpMessage));
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

            return new MethodRequestInternal(methodName, methodRequestId, amqpMessage.BodyStream);
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

        public static ArraySegment<byte> ReadStream(Stream stream)
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
    }
}
