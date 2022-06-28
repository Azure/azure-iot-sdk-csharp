// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Options that allow configuration of the device or module client instance during initialization.
    /// Updating these options after the client has been initialized will not change the behavior of the client.
    /// </summary>
    public class ClientOptions
    {
        /// <summary>
        /// Initializes an instance of <see cref="ClientOptions"/>.
        /// </summary>
        /// <param name="payloadConvention">
        /// The payload convention to be used to serialize and encode the messages for convention based methods.
        /// The default value is set to <see cref="DefaultPayloadConvention"/> which uses the <see cref="NewtonsoftJsonPayloadSerializer"/> serializer
        /// and <see cref="Utf8PayloadEncoder"/> encoder.
        /// </param>
        public ClientOptions(PayloadConvention payloadConvention = default)
        {
            PayloadConvention = payloadConvention ?? DefaultPayloadConvention.Instance;
        }

        /// <summary>
        /// The DTDL model Id associated with the device or module client instance.
        /// This feature is currently supported only over MQTT and AMQP.
        /// </summary>
        public string ModelId { get; set; }

        /// <summary>
        /// The transport settings to use for all file upload operations, regardless of what protocol the device
        /// client is configured with. All file upload operations take place over https.
        /// If FileUploadTransportSettings is not provided, then file upload operations will use the same client certificates
        /// configured in the transport settings set for client connect.
        /// </summary>
        public Http1TransportSettings FileUploadTransportSettings { get; set; } = new Http1TransportSettings();

        /// <summary>
        /// The configuration for setting <see cref="MessageBase.MessageId"/> for every message sent by the device or module client instance.
        /// The default behavior is that <see cref="MessageBase.MessageId"/> is set only by the user.
        /// </summary>
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

        /// <summary>
        /// The payload convention to be used to serialize and encode the messages for convention based methods.
        /// </summary>
        /// <remarks>
        /// The <see cref="Shared.PayloadConvention"/> defines both the serializer and encoding to be used for convention based messages.
        /// You will only need to set this if you have objects that have special serialization rules or require a specific byte encoding.
        /// <para>
        /// The default value is set to <see cref="DefaultPayloadConvention"/> which uses the <see cref="NewtonsoftJsonPayloadSerializer"/> serializer
        /// and <see cref="Utf8PayloadEncoder"/> encoder.
        /// </para>
        /// </remarks>
        public PayloadConvention PayloadConvention { get; }
    }
}
