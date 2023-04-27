// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The optional configuration of the device or module client instances.
    /// </summary>
    public sealed class IotHubClientOptions
    {
        /// <summary>
        /// Creates an instances of this class with default transport (MQTT TCP) settings.
        /// </summary>
        public IotHubClientOptions()
            : this(new IotHubClientMqttSettings())
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified transport settings.
        /// </summary>
        /// <param name="transportSettings">The transport settings to use (i.e., <see cref="IotHubClientMqttSettings"/> or
        /// <see cref="IotHubClientAmqpSettings"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="transportSettings"/> is null.</exception>
        /// <example>
        /// <code language="csharp">
        /// await using var client = new IotHubDeviceClient(
        ///     connectionString,
        ///     new IotHubClientOptions(new IotHubClientMqttSettings(IotHubClientTransportProtocol.WebSocket))
        ///     {
        ///         PayloadConvention = SystemTextJsonPayloadConvention.Instance,
        ///         GatewayHostName = "myIotEdgeGateway.contoso.com",
        ///         SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
        ///         RetryPolicy = new IotHubClientFixedDelayRetryPolicy(maxRetries: 10, fixedDelay: TimeSpan.FromSeconds(3), userJitter: true),
        ///     });
        /// </code>
        /// </example>
        public IotHubClientOptions(IotHubClientTransportSettings transportSettings)
        {
            TransportSettings = transportSettings ?? throw new ArgumentNullException(nameof(transportSettings));
            if (transportSettings is not IotHubClientMqttSettings
                && transportSettings is not IotHubClientAmqpSettings)
            {
                throw new ArgumentException("Transport settings must be MQTT or AMQP.", nameof(transportSettings));
            }
        }

        /// <summary>
        /// The transport settings to use (i.e., <see cref="IotHubClientMqttSettings"/> or <see cref="IotHubClientAmqpSettings"/>).
        /// </summary>
        public IotHubClientTransportSettings TransportSettings { get; }

        /// <summary>
        /// The transport settings to use for all HTTP requests made by this client.
        /// </summary>
        /// <remarks>
        /// All <see cref="IotHubDeviceClient"/> file upload operations take place over HTTP regardless of the configured protocol. 
        /// Additionally, all <see cref="IotHubModuleClient"/> direct method invoking operations (such as 
        /// <see cref="IotHubModuleClient.InvokeMethodAsync(string, DirectMethodRequest, System.Threading.CancellationToken)"/>) 
        /// take place over HTTP as well. The settings provided in this class will be used for all these operations.
        /// </remarks>
        public IotHubClientHttpSettings HttpOperationTransportSettings { get; set; } = new IotHubClientHttpSettings();

        /// <summary>
        /// The payload convention to be used to serialize and encode JSON payloads exchanged with the service.
        /// </summary>
        /// <remarks>
        /// The payload convention defines the serializer and deserializer be used. It only needs to be set if there are objects
        /// that have special serialization rules or require a specific byte encoding.
        /// <para>
        /// The default value is set to <see cref="DefaultPayloadConvention"/> which uses
        /// <see href="https://www.nuget.org/packages/Newtonsoft.Json"/> and UTF-8 encoding.
        /// </para>
        /// </remarks>
        public PayloadConvention PayloadConvention { get; set; } = DefaultPayloadConvention.Instance;

        /// <summary>
        /// The fully-qualified DNS host name of a gateway to connect through.
        /// </summary>
        /// <remarks>
        /// Set this property to specify the FQDN of that gateway if the device or device module connects to IoT hub through an
        /// IoT Edge gateway.
        /// <para>
        /// It can also be used for other, custom transparent or protocol gateways.
        /// </para>
        /// </remarks>
        public string GatewayHostName { get; set; }

        /// <summary>
        /// The DTDL model Id associated with the device or module client instance.
        /// </summary>
        /// <seealso href="https://learn.microsoft.com/azure/iot-develop/concepts-modeling-guide"/>
        public string ModelId { get; set; }

        /// <summary>
        /// The configuration for setting <see cref="TelemetryMessage.MessageId"/> for every message sent by the device or module client instance.
        /// </summary>
        /// <remarks>
        /// The default behavior is that a message Id is only sent if set by the user.
        /// </remarks>
        public SdkAssignsMessageId SdkAssignsMessageId { get; set; } = SdkAssignsMessageId.Never;

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <remarks>
        /// Defaults to a nearly infinite exponential backoff.
        /// <para>
        /// If set to null, will use <see cref="IotHubClientNoRetry"/> to perform no retries.
        /// </para>
        /// <para>
        /// It can be set to any of the built-in retry policies such as <see cref="IotHubClientExponentialBackoffRetryPolicy"/>,
        /// <see cref="IotHubClientFixedDelayRetryPolicy"/>, or <see cref="IotHubClientIncrementalDelayRetryPolicy"/>,
        /// or a custom one by inheriting from <see cref="IIotHubClientRetryPolicy"/>.
        /// </para>
        /// </remarks>
        public IIotHubClientRetryPolicy RetryPolicy { get; set; } = new IotHubClientExponentialBackoffRetryPolicy(0, TimeSpan.FromHours(12), true);

        /// <summary>
        /// Specifies additional information that will be appended to the user-agent string that is sent to IoT hub.
        /// </summary>
        public string AdditionalUserAgentInfo
        {
            get => UserAgentInfo.Extra;
            set => UserAgentInfo.Extra = value;
        }

        internal ProductInfo UserAgentInfo { get; } = new();

        internal IotHubClientOptions Clone()
        {
            IotHubClientTransportSettings transport = TransportSettings.Clone();

            return new IotHubClientOptions(transport)
            {
                HttpOperationTransportSettings = (IotHubClientHttpSettings)HttpOperationTransportSettings.Clone(),
                PayloadConvention = PayloadConvention,
                GatewayHostName = GatewayHostName,
                ModelId = ModelId,
                SdkAssignsMessageId = SdkAssignsMessageId,
                AdditionalUserAgentInfo = AdditionalUserAgentInfo,
                RetryPolicy = RetryPolicy,
            };
        }
    }
}
