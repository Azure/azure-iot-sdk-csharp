// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Options that allow configuration of the IoT hub device or module client instance during initialization.
    /// </summary>
    public class IotHubClientOptions
    {
        /// <summary>
        /// Creates an instances of this class with the default transport settings.
        /// </summary>
        public IotHubClientOptions()
        {
            TransportSettings = new IotHubClientAmqpSettings();
        }

        /// <summary>
        /// Creates an instance of this class with the specified transport settings.
        /// </summary>
        /// <param name="transportSettings">The transport settings to use (i.e., <see cref="IotHubClientMqttSettings"/>,
        /// <see cref="IotHubClientAmqpSettings"/>, or <see cref="IotHubClientHttpSettings"/>).</param>
        /// <exception cref="ArgumentNullException">When <paramref name="transportSettings"/> is null.</exception>
        public IotHubClientOptions(IotHubClientTransportSettings transportSettings)
        {
            TransportSettings = transportSettings ?? throw new ArgumentNullException(nameof(transportSettings));
        }

        /// <summary>
        /// The transport settings to use (i.e., <see cref="IotHubClientMqttSettings"/>, <see cref="IotHubClientAmqpSettings"/>, or <see cref="IotHubClientHttpSettings"/>).
        /// </summary>
        public IotHubClientTransportSettings TransportSettings { get; }

        /// <summary>
        /// The transport settings to use for all file upload operations, regardless of what protocol the device
        /// client is configured with. All file upload operations take place over https.
        /// If FileUploadTransportSettings is not provided, then file upload operations will use the same client certificates
        /// configured in the transport settings set for client connect.
        /// </summary>
        public IotHubClientHttpSettings FileUploadTransportSettings { get; set; } = new IotHubClientHttpSettings();

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
        /// <see cref="IotHubDeviceClient.CreateFromConnectionString(string, IotHubClientOptions)"/> flow, the <see cref="IotHubModuleClient.CreateFromConnectionString(string, IotHubClientOptions)"/> flow
        /// or the <see cref="IotHubModuleClient.CreateFromEnvironmentAsync(IotHubClientOptions)"/> flow.
        /// </remarks>
        public TimeSpan SasTokenTimeToLive { get; set; }

        /// <summary>
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live. Acceptable values lie between 0 and 100 (including the endpoints).
        /// Eg. if set to a value of 30, the token will be renewed when it has 30% or less of its lifespan left.
        /// If unset the token will be renewed when it has 15% or less of its lifespan left.
        /// </summary>
        /// <remarks>
        /// This is used only for SAS token authenticated clients through either the
        /// <see cref="IotHubDeviceClient.CreateFromConnectionString(string, IotHubClientOptions)"/> flow, the <see cref="IotHubModuleClient.CreateFromConnectionString(string, IotHubClientOptions)"/> flow
        /// or the <see cref="IotHubModuleClient.CreateFromEnvironmentAsync(IotHubClientOptions)"/> flow.
        /// </remarks>
        public int SasTokenRenewalBuffer { get; set; }
    }
}
