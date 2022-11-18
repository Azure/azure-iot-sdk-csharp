// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service enrollment and their accessors with a JSON serializer
    /// and deserializer.
    /// </summary>
    /// <remarks>
    /// This object is used to send and receive individualEnrollment information to and from the provisioning service.
    ///
    /// To create or update an Enrollment on the provisioning service you should fill this object and call the
    /// public API <see cref="IndividualEnrollmentsClient.CreateOrUpdateAsync(IndividualEnrollment, CancellationToken)"/>.
    ///
    /// The minimum information required by the provisioning service is the RegistrationId and the
    /// Attestation.
    ///
    /// A new device can be provisioned by three attestation mechanisms, X509
    /// (<see cref="X509Attestation"/>), Symmetric Key (see <see cref="SymmetricKeyAttestation"/>,
    /// and TPM (<see cref="TpmAttestation"/>). The definition of each one you
    /// should use depending on the physical authentication hardware that the device contains.
    ///
    /// The content of this class will be serialized in a JSON format and sent as a body of the rest API to the
    /// provisioning service. Or the content of this class can be filled by a JSON, received from the provisioning
    /// service, as result of a individualEnrollment operation like create, update, or query.
    /// </remarks>
    public class IndividualEnrollment
    {
        /// <summary>
        /// Creates a new instance of IndividualEnrollment.
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the IndividualEnrollment object with the minimum set of
        /// information required by the provisioning service. A valid individualEnrollment must contain the
        /// registrationId, which uniquely identify this enrollment, and the attestation mechanism, which can
        /// be X509, or Symmetric key.
        ///
        /// Other parameters can be added by calling the setters on this object.
        /// </remarks>
        /// <param name="registrationId">The string that uniquely identify this enrollment in the provisioning
        /// service. It cannot be null or empty.</param>
        /// <param name="attestation">The <see cref="Attestation"/> object with the attestation mechanism.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="registrationId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="registrationId"/> is empty or white space.</exception>
        public IndividualEnrollment(string registrationId, Attestation attestation)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));
            RegistrationId = registrationId;
            Attestation = attestation;
        }

        // This JsonConstructor is used for serialization instead of the usual empty constructor
        // because one of this object's fields (attestation) doesn't map 1:1 with where that field
        // is in the JSON the service sends.
        [JsonConstructor]
        internal IndividualEnrollment(
            string registrationId,
            AttestationMechanism attestation,
            string deviceId,
            string iotHubHostName,
            ProvisioningTwinState initialTwinState,
            ProvisioningStatus? provisioningStatus,
            DateTimeOffset createdOnUtc,
            DateTimeOffset lastUpdatedOnUtc,
            ETag eTag,
            ProvisioningClientCapabilities capabilities)
        {
            if (attestation == null)
            {
                throw new ProvisioningServiceException("Service responded with an enrollment without attestation.", HttpStatusCode.BadRequest);
            }

            RegistrationId = registrationId;
            DeviceId = deviceId;
            Attestation = attestation.GetAttestation(); // This is the one reason why we can't use an empty constructor here.
            IotHubHostName = iotHubHostName;
            InitialTwinState = initialTwinState;
            ProvisioningStatus = provisioningStatus;
            CreatedOnUtc = createdOnUtc;
            LastUpdatedOnUtc = lastUpdatedOnUtc;
            ETag = eTag;
            Capabilities = capabilities;
        }

        /// <summary>
        /// Registration Id.
        /// </summary>
        /// <remarks>
        /// A valid registration Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided string does not fit the registration Id requirements</exception>
        [JsonPropertyName("registrationId")]
        public string RegistrationId { get; internal set; }

        /// <summary>
        /// Desired IoT hub device Id (optional).
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; }

        /// <summary>
        /// Current registration state.
        /// </summary>
        [JsonPropertyName("registrationState")]
        public DeviceRegistrationState RegistrationState { get; internal set; }

        /// <summary>
        /// Attestation mechanism.
        /// </summary>
        [JsonPropertyName("attestation")]
        private AttestationMechanism _attestation;

        /// <summary>
        /// Attestation.
        /// </summary>
        [JsonIgnore]
        public Attestation Attestation
        {
            get => _attestation?.GetAttestation();

            set
            {
                if (value is X509Attestation attestation)
                {
                    if ((attestation ?? throw new InvalidOperationException(nameof(value))).ClientCertificates == null
                        && attestation.CaReferences == null)
                    {
                        throw new InvalidOperationException($"Value for {nameof(attestation)} does not contain client certificate or CA reference.");
                    }
                }

                if (value != null)
                { 
                    _attestation = new AttestationMechanism(value);
                }
            }
        }

        /// <summary>
        /// Desired IoT hub to assign the device to.
        /// </summary>
        [JsonPropertyName("iotHubHostName")]
        public string IotHubHostName { get; set; }

        /// <summary>
        /// Initial twin state.
        /// </summary>
        [JsonPropertyName("initialTwin")]
        public ProvisioningTwinState InitialTwinState { get; set; }

        /// <summary>
        /// The provisioning status.
        /// </summary>
        [JsonConverter(typeof(JsonStringEnumConverter))]
        [JsonPropertyName("provisioningStatus")]
        public ProvisioningStatus? ProvisioningStatus { get; set; }

        /// <summary>
        /// The DateTime this resource was created.
        /// </summary>
        [JsonPropertyName("createdDateTimeUtc")]
        public DateTimeOffset? CreatedOnUtc { get; internal set; }

        /// <summary>
        /// The DateTime this resource was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdatedDateTimeUtc")]
        public DateTimeOffset? LastUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Enrollment's ETag.
        /// </summary>
        [JsonPropertyName("etag")]
        public ETag ETag { get; set; }

        /// <summary>
        /// Capabilities of the device.
        /// </summary>
        [JsonPropertyName("capabilities")]
        public ProvisioningClientCapabilities Capabilities { get; set; }

        /// <summary>
        /// The behavior when a device is re-provisioned to an IoT hub.
        /// </summary>
        [JsonPropertyName("reprovisionPolicy")]
        public ReprovisionPolicy ReprovisionPolicy { get; set; }

        /// <summary>
        /// Custom allocation definition.
        /// </summary>
        [JsonPropertyName("customAllocationDefinition")]
        public CustomAllocationDefinition CustomAllocationDefinition { get; set; }

        /// <summary>
        /// The allocation policy of this resource. Overrides the tenant level allocation policy.
        /// </summary>
        [JsonPropertyName("allocationPolicy")]
        public AllocationPolicy? AllocationPolicy { get; set; }

        /// <summary>
        /// The list of names of IoT hubs the device in this resource can be allocated to. Must be a subset of tenant level list of IoT hubs.
        /// </summary>
        [JsonPropertyName("iotHubs")]
        public IList<string> IotHubs { get; set; } = new List<string>();

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The string with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(this, Formatting.Indented);
        }

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool ShouldSerializeIotHubs()
        {
            return IotHubs != null && IotHubs.Any();
        }
    }
}
