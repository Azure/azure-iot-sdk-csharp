﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The names of the system properties in the <see cref="Message"/> class.
    /// </summary>
    public static class MessageSystemPropertyNames
    {
        /// <summary>
        /// A user-settable identifier for the messages. If this value is not supplied by the user,
        /// the service client will set this to a new GUID only if you set <see cref="ServiceClientOptions.SdkAssignsMessageId"/>
        /// property in <see cref="ServiceClientOptions"/>.
        /// </summary>
        public const string MessageId = "message-id";

        /// <summary>
        /// Lock token of the received message.
        /// A unique identifier for a cloud-to-device message used to complete, reject or abandon the message.
        /// This value is provided to resolve race conditions when completing, rejecting, or abandoning messages.
        /// </summary>
        /// <remarks>
        /// If the lock token expires, the cloud-to-device message needs to be received again to complete, reject or abandon
        /// the message without errors.
        /// </remarks>
        public const string LockToken = "iothub-messagelocktoken";

        /// <summary>
        /// A number (unique per device-queue) assigned by IoT hub to each cloud-to-device message.
        /// </summary>
        public const string SequenceNumber = "iothub-sequencenumber";

        /// <summary>
        /// A destination specified in cloud-to-device messages.
        /// </summary>
        public const string To = "to";

        /// <summary>
        /// The number of times a message can transition between the Enqueued and Invisible states.
        /// After the maximum number of transitions, the IoT hub sets the state of the message to dead-lettered.
        /// For more information, see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#the-cloud-to-device-message-life-cycle"/>
        /// </summary>
        public const string DeliveryCount = "iothub-deliverycount";

        /// <summary>
        /// Date and time when the message was received by the server in UTC.
        /// </summary>
        public const string EnqueuedTime = "iothub-enqueuedtime";

        /// <summary>
        /// Date and time of message expiration in UTC.
        /// </summary>
        public const string ExpiryTimeUtc = "absolute-expiry-time";

        /// <summary>
        /// A string property in a response message that typically contains the MessageId of the request, in request-reply patterns.
        /// </summary>
        public const string CorrelationId = "correlation-id";

        /// <summary>
        /// An Id used to specify the origin of messages. When messages are generated by IoT Hub, it is set to name of the IoT hub.
        /// </summary>
        public const string UserId = "user-id";

        /// <summary>
        /// IoT hub operation
        /// </summary>
        public const string Operation = "iothub-operation";

        /// <summary>
        /// A feedback message generator. This property is used in cloud-to-device messages to request IoT hub to generate feedback messages as a result of the consumption of the message by the device.
        /// </summary>
        /// <remarks>
        /// Possible values:
        /// <list type="bullet">
        /// <item>
        /// <description>none (default): no feedback message is generated.</description>
        /// </item>
        /// <item>
        /// <description>positive: receive a feedback message if the message was completed.</description>
        /// </item>
        /// <item>
        /// <description>negative: receive a feedback message if the message expired (or maximum delivery count was reached) without being completed by the device.</description>
        /// </item>
        /// <item>
        /// <description>full: both positive and negative.</description>
        /// </item>
        /// </list>
        /// </remarks>
        public const string Ack = "iothub-ack";

        /// <summary>
        /// Specifies the device Id from which this message was sent, if there is one.
        /// </summary>
        public const string ConnectionDeviceId = "iothub-connection-device-id";

        /// <summary>
        /// The device generation Id of the target device of the cloud-to-device message.
        /// DeviceGenerationId is an IoT hub generated, case-sensitive string. This value is used to distinguish devices with the
        /// same device Id when they have been deleted and re-created.
        /// </summary>
        public const string ConnectionDeviceGenerationId = "iothub-connection-auth-generation-id";

        /// <summary>
        /// The connection authentication method value is ignored for cloud-to-device messages.
        /// </summary>
        public const string ConnectionAuthMethod = "iothub-connection-auth-method";

        /// <summary>
        /// The message schema is set internally by IoTHub when it generates Twin Change notification message.
        /// </summary>
        public const string MessageSchema = "iothub-message-schema";

        /// <summary>
        /// Custom date property set by the originator of the message.
        /// </summary>
        public const string CreationTimeUtc = "iothub-creation-time-utc";

        /// <summary>
        /// Used to specify the content encoding type of the message.
        /// Possible values are: utf-8, utf-16, utf-32.
        /// </summary>
        public const string ContentEncoding = "iothub-content-encoding";

        /// <summary>
        /// Used to specify the content type of the message.
        /// Possible values are: application/json, application/json-patch+json.
        /// </summary>
        public const string ContentType = "iothub-content-type";
    }
}
