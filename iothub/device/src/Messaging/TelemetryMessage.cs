// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure that represents the message to be sent to IoT hub.
    /// </summary>
    public class TelemetryMessage
    {
        /// <summary>
        /// Crates an instance of this class with no telemetry payload.
        /// </summary>
        public TelemetryMessage()
        { }

        /// <summary>
        /// Creates an instance of this class with the specified binary payload.
        /// </summary>
        /// <param name="binaryPayload">The binary payload to send.</param>
        public TelemetryMessage(byte[] binaryPayload)
        {
            Payload = binaryPayload;
        }

        /// <summary>
        /// The message payload.
        /// </summary>
        /// <remarks>
        ///  Use <see cref="SetPayload(object)"/> to set this payload as a strongly typed object (that is serializable by System.Text.Json)
        /// </remarks>
        public byte[] Payload { get; set; }

        /// <summary>
        /// An identifier for the message useful for avoiding reprocessing the same message again.
        /// </summary>
        /// <remarks>
        /// Format: A case-sensitive string (up to 128 char long) of ASCII 7-bit alphanumeric chars
        /// plus these non-alphanumeric characters:
        /// { '-', ':', '/', '\', '.', '+', '%', '_', '#', '*', '?', '!', '(', ')', ',', '=', '@', ';', '$', ''' }.
        /// </remarks>
        public string MessageId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.MessageId);
            set => SystemProperties[MessageSystemPropertyNames.MessageId] = value;
        }

        /// <summary>
        /// A string property of the request useful for tracking specific messages across device
        /// clients, Edge modules, and service clients.
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
            set => SystemProperties[MessageSystemPropertyNames.ContentType] = value;
        }

        /// <summary>
        /// Used to specify the content encoding type of the message.
        /// </summary>
        public string ContentEncoding
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ContentEncoding);
            set => SystemProperties[MessageSystemPropertyNames.ContentEncoding] = value;
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
        /// Specifies the output name on which the message will be sent, if applicable.
        /// </summary>
        /// <remarks>
        /// Used for message routes with IoT Edge.
        /// </remarks>
        public string OutputName
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.OutputName);
            set => SystemProperties[MessageSystemPropertyNames.OutputName] = value;
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
        /// Set this payload as an arbitrary JSON document.
        /// </summary>
        /// <param name="payload">The JSON value string. For instance "{\"someKey\":\"someValue\"}"</param>
        /// <remarks>This function just UTF-8 encodes the provided string. It does not further validation.</remarks>
        public void SetPayload(string payload)
        {
            Payload = Encoding.UTF8.GetBytes(payload);
        }

        /// <summary>
        /// Use a serializable object as the payload.
        /// </summary>
        /// <param name="serializableObject">Any custom payload object that is serializable by System.Text.Json</param>
        /// <remarks>
        /// This object must be serializable by System.Text.Json
        /// </remarks>
        public void SetPayload(object serializableObject)
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(serializableObject);
        }

        /// <summary>
        /// Gets the dictionary of system properties which are managed internally.
        /// </summary>
        protected internal IDictionary<string, object> SystemProperties { get; private set; } = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Sets the message as an security message
        /// </summary>
        public void SetAsSecurityMessage()
        {
            SystemProperties[MessageSystemPropertyNames.InterfaceId] = CommonConstants.SecurityMessageInterfaceId;
        }

        private T GetSystemProperty<T>(string key)
        {
            return SystemProperties.TryGetValue(key, out object value) ? (T)value : default;
        }
    }
}
