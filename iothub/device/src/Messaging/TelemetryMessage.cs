// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure that represents the message that will be sent to IoT hub.
    /// </summary>
    public class TelemetryMessage
    {
        /// <summary>
        /// Default instantiation with no payload.
        /// </summary>
        public TelemetryMessage()
        { }

        /// <summary>
        /// Creates an outgoing message with the specified payload.
        /// </summary>
        /// <remarks>
        /// The payload will be serialized and encoded per <see cref="IotHubClientOptions.PayloadConvention"/>.
        /// </remarks>
        /// <param name="payload">The payload to send.</param>
        public TelemetryMessage(object payload)
        {
            Payload = payload;
        }

        /// <summary>
        /// The message payload.
        /// </summary>
        public object Payload { get; }

        /// <summary>
        /// An identifier for the message used for request-reply patterns.
        /// Format: A case-sensitive string ( up to 128 char long) of ASCII 7-bit alphanumeric chars
        /// + {'-', ':', '/', '\', '.', '+', '%', '_', '#', '*', '?', '!', '(', ')', ',', '=', '@', ';', '$', '''}.
        /// Non-alphanumeric characters are from URN RFC.
        /// </summary>
        public string MessageId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.MessageId);
            set => SystemProperties[MessageSystemPropertyNames.MessageId] = value;
        }

        /// <summary>
        /// A string property that typically contains the MessageId of the request, in request-reply patterns.
        /// </summary>
        public string CorrelationId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.CorrelationId);
            set => SystemProperties[MessageSystemPropertyNames.CorrelationId] = value;
        }

        /// <summary>
        /// An Id used to specify the origin of messages.
        /// </summary>
        public string UserId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.UserId);
            set => SystemProperties[MessageSystemPropertyNames.UserId] = value;
        }

        /// <summary>
        /// The event creation time when sending one message at a time.
        /// </summary>
        public DateTimeOffset CreatedOnUtc
        {
            get => GetSystemProperty<DateTimeOffset>(MessageSystemPropertyNames.CreationTimeUtc);
            set => SystemProperties[MessageSystemPropertyNames.CreationTimeUtc] = value;
        }

        /// <summary>
        /// The event creation time when sending data in a batch.
        /// </summary>
        public DateTimeOffset BatchCreatedOnUtc
        {
            get => GetSystemProperty<DateTimeOffset>(MessageSystemPropertyNames.CreationTimeBatchUtc);
            set => SystemProperties[MessageSystemPropertyNames.CreationTimeBatchUtc] = value;
        }

        /// <summary>
        /// The time when this message is considered expired.
        /// </summary>
        public DateTimeOffset ExpiresOnUtc
        {
            get => GetSystemProperty<DateTimeOffset>(MessageSystemPropertyNames.ExpiryTimeUtc);
            set => SystemProperties[MessageSystemPropertyNames.ExpiryTimeUtc] = value;
        }

        /// <summary>
        /// The DTDL component name from where the telemetry message has originated.
        /// This is relevant only for plug and play certified devices.
        /// </summary>
        public string ComponentName
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ComponentName);
            set => SystemProperties[MessageSystemPropertyNames.ComponentName] = value;
        }

        /// <summary>
        /// Used to specify the schema of the message content.
        /// </summary>
        public string MessageSchema
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.MessageSchema);
            set => SystemProperties[MessageSystemPropertyNames.MessageSchema] = value;
        }

        /// <summary>
        /// Used to specify the content type of the message.
        /// </summary>
        public string ContentType
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ContentType);
            protected internal set => SystemProperties[MessageSystemPropertyNames.ContentType] = value;
        }

        /// <summary>
        /// Used to specify the content encoding type of the message.
        /// </summary>
        public string ContentEncoding
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ContentEncoding);
            protected internal set => SystemProperties[MessageSystemPropertyNames.ContentEncoding] = value;
        }

        /// <summary>
        /// Date and time when the device-to-cloud message was received by the server.
        /// </summary>
        public DateTimeOffset EnqueuedOnUtc
        {
            get => GetSystemProperty<DateTimeOffset>(MessageSystemPropertyNames.EnqueuedTime);
            protected internal set => SystemProperties[MessageSystemPropertyNames.EnqueuedTime] = value;
        }

        /// <summary>
        /// Specifies the device Id from which this message was sent, if there is one.
        /// </summary>
        public string ConnectionDeviceId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ConnectionDeviceId);
            protected internal set => SystemProperties[MessageSystemPropertyNames.ConnectionDeviceId] = value;
        }

        /// <summary>
        /// Specifies the module Id from which this message was sent, if there is one.
        /// </summary>
        public string ConnectionModuleId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ConnectionModuleId);
            protected internal set => SystemProperties[MessageSystemPropertyNames.ConnectionModuleId] = value;
        }

        /// <summary>
        /// Specifies the input name on which the message was sent, if there was one.
        /// </summary>
        public string InputName
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.InputName);
            protected internal set => SystemProperties[MessageSystemPropertyNames.InputName] = value;
        }

        /// <summary>
        /// True if the message is set as a security message
        /// </summary>
        public bool IsSecurityMessage => CommonConstants.SecurityMessageInterfaceId.Equals(
            GetSystemProperty<string>(MessageSystemPropertyNames.InterfaceId),
            StringComparison.Ordinal);

        /// <summary>
        /// Gets the dictionary of user properties which are set when user sends the data.
        /// </summary>
        public IDictionary<string, string> Properties { get; private set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets the dictionary of system properties which are managed internally.
        /// </summary>
        protected internal IDictionary<string, object> SystemProperties { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The convention to use with this message payload.
        /// </summary>
        protected internal PayloadConvention PayloadConvention { get; set; } = DefaultPayloadConvention.Instance;

        /// <summary>
        /// Clones an existing <see cref="Message"/> instance and sets content body defined by <paramref name="payload"/> on it.
        /// </summary>
        /// <remarks>
        /// The cloned message has the message <see cref="MessageId" /> as the original message.
        /// </remarks>
        /// <param name="payload">Message content to be set after clone.</param>
        /// <returns>A new instance of <see cref="Message"/> with body content defined by <paramref name="payload"/>,
        /// and user/system properties of the cloned <see cref="Message"/> instance.
        /// </returns>
        public TelemetryMessage CloneWithBody(object payload)
        {
            var result = new TelemetryMessage(payload);

            foreach (string key in Properties.Keys)
            {
                result.Properties.Add(key, Properties[key]);
            }

            foreach (string key in SystemProperties.Keys)
            {
                result.SystemProperties.Add(key, SystemProperties[key]);
            }

            return result;
        }

        /// <summary>
        /// Sets the message as an security message
        /// </summary>
        public void SetAsSecurityMessage()
        {
            SystemProperties[MessageSystemPropertyNames.InterfaceId] = CommonConstants.SecurityMessageInterfaceId;
        }

        /// <summary>
        /// Gets the payload as a byte array.
        /// </summary>
        /// <remarks>
        /// This will get the fully encoded serialized string using both <see cref="PayloadSerializer.SerializeToString(object)"/>.
        /// and <see cref="PayloadEncoder.EncodeStringToByteArray(string)"/> methods implemented in the <see cref="PayloadConvention"/>.
        /// </remarks>
        /// <returns>A fully encoded serialized string.</returns>
        internal byte[] GetPayloadObjectBytes()
        {
            return PayloadConvention.GetObjectBytes(Payload);
        }

        private T GetSystemProperty<T>(string key)
        {
            return SystemProperties.ContainsKey(key)
                ? (T)SystemProperties[key]
                : default;
        }
    }
}
