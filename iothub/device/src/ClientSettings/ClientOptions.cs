// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Options that allow configuration of the device or module client instance during initialization.
    /// </summary>
    public class ClientOptions
    {
        /// <summary>
        /// The transport type to use (i.e., AMQP, MQTT, HTTP), including whether to use TCP or web sockets where applicable.
        /// </summary>
        public TransportType TransportType { get; set; } = TransportType.Amqp_Tcp_Only;

        /// <summary>
        /// The transport settings to use (i.e., <see cref="MqttTransportSettings"/>, <see cref="AmqpTransportSettings"/>, or <see cref="HttpTransportSettings"/>).
        /// </summary>
        public ITransportSettings TransportSettings { get; set; }

        /// <summary>
        /// The transport settings to use for all file upload operations, regardless of what protocol the device
        /// client is configured with. All file upload operations take place over https.
        /// If FileUploadTransportSettings is not provided, then file upload operations will use the same client certificates
        /// configured in the transport settings set for client connect.
        /// </summary>
        public HttpTransportSettings FileUploadTransportSettings { get; set; } = new HttpTransportSettings();

        /// <summary>
        /// The fully-qualified DNS host name of a gateway to connect through.
        /// </summary>
        public string GatewayHostName { get; set; }

        /// <summary>
        /// The DTDL model Id associated with the device or module client instance.
        /// </summary>
        /// This feature is currently supported only over MQTT and AMQP transports.
        /// <remarks></remarks>
        public string ModelId { get; set; }

        /// <summary>
        /// The configuration for setting <see cref="Message.MessageId"/> for every message sent by the device or module client instance.
        /// </summary>
        /// <remarks>
        /// The default behavior is that MessageId is set only by the user.
        /// </remarks>
        public SdkAssignsMessageId SdkAssignsMessageId { get; set; } = SdkAssignsMessageId.Never;

        /// <summary>
        /// The suggested time to live value for tokens generated for SAS authenticated clients.
        /// The <see cref="TimeSpan"/> provided should be a positive value, signifying that it is not possible to generate tokens that have already expired.
        /// If unset the generated SAS tokens will be valid for 1 hour.
        /// </summary>
        /// <remarks>
        /// This is used only for SAS token authenticated clients through either the
        /// <see cref="DeviceClient.CreateFromConnectionString(string, ClientOptions)"/> flow, the <see cref="ModuleClient.CreateFromConnectionString(string, ClientOptions)"/> flow
        /// or the <see cref="ModuleClient.CreateFromEnvironmentAsync(ClientOptions)"/> flow.
        /// </remarks>
        public TimeSpan SasTokenTimeToLive { get; set; }

        /// <summary>
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live. Acceptable values lie between 0 and 100 (including the endpoints).
        /// Eg. if set to a value of 30, the token will be renewed when it has 30% or less of its lifespan left.
        /// If unset the token will be renewed when it has 15% or less of its lifespan left.
        /// </summary>
        /// <remarks>
        /// This is used only for SAS token authenticated clients through either the
        /// <see cref="DeviceClient.CreateFromConnectionString(string, ClientOptions)"/> flow, the <see cref="ModuleClient.CreateFromConnectionString(string, ClientOptions)"/> flow
        /// or the <see cref="ModuleClient.CreateFromEnvironmentAsync(ClientOptions)"/> flow.
        /// </remarks>
        public int SasTokenRenewalBuffer { get; set; }
    }
}
