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
            : this(new IotHubClientMqttSettings())
        {
        }

        /// <summary>
        /// Creates an instance of this class with the specified transport settings.
        /// </summary>
        /// <param name="transportSettings">The transport settings to use (i.e., <see cref="IotHubClientMqttSettings"/> or
        /// <see cref="IotHubClientAmqpSettings"/>.</param>
        /// <exception cref="ArgumentNullException">When <paramref name="transportSettings"/> is null.</exception>
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
        /// The transport settings to use for all file upload operations, regardless of what protocol the device
        /// client is configured with. All file upload operations take place over https.
        /// </summary>
        public IotHubClientHttpSettings FileUploadTransportSettings { get; set; } = new IotHubClientHttpSettings();

        /// <summary>
        /// The payload convention to be used to serialize and encode the payload being sent to service.
        /// </summary>
        /// <remarks>
        /// The <see cref="PayloadConvention"/> defines both the serializer and encoding to be used.
        /// You will only need to set this if you have objects that have special serialization rules or require a specific byte encoding.
        /// <para>
        /// The default value is set to <see cref="DefaultPayloadConvention"/> which uses the <see cref="NewtonsoftJsonPayloadSerializer"/> serializer
        /// and <see cref="Utf8PayloadEncoder"/> encoder.
        /// </para>
        /// </remarks>
        public PayloadConvention PayloadConvention { get; set; } = DefaultPayloadConvention.Instance;

        /// <summary>
        /// The fully-qualified DNS host name of a gateway to connect through.
        /// </summary>
        public string GatewayHostName { get; set; }

        /// <summary>
        /// The DTDL model Id associated with the device or module client instance.
        /// </summary>
        /// <remarks>
        /// This feature is currently supported only over MQTT and AMQP transports.
        /// </remarks>
        public string ModelId { get; set; }

        /// <summary>
        /// The configuration for setting <see cref="Message.MessageId"/> for every message sent by the device or module client instance.
        /// </summary>
        /// <remarks>
        /// The default behavior is that <see cref="Message.MessageId"/> is set only by the user.
        /// </remarks>
        public SdkAssignsMessageId SdkAssignsMessageId { get; set; } = SdkAssignsMessageId.Never;

        /// <summary>
        /// Sets the retry policy used in the operation retries.
        /// </summary>
        /// <remarks>
        /// Defaults to a near infinite exponential backoff. If set to null, will use <see cref="NoRetry"/> to perform no retries
        /// </remarks>
        public IRetryPolicy RetryPolicy { get; set; } = new ExponentialBackoff(int.MaxValue, TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(10), TimeSpan.FromMilliseconds(100));

        /// <summary>
        /// Specifies additional information that will be appended to the user-agent string that is sent to IoT hub.
        /// </summary>
        public string AdditionalUserAgentInfo
        {
            get => UserAgentInfo.Extra;
            set => UserAgentInfo.Extra = value;
        }

        internal virtual ProductInfo UserAgentInfo { get; } = new();
    }
}
