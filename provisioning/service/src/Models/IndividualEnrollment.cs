// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
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
        /// For deserialization.
        /// </summary>
        internal IndividualEnrollment()
        { }

        /// <summary>
        /// Creates an instance of this class with the required fields.
        /// </summary>
        /// <param name="registrationId">The Id of the registration.</param>
        /// <param name="attestation">The attestation.</param>
        /// <exception cref="ArgumentException">When <paramref name="attestation"/> is an unsupported type.</exception>
        public IndividualEnrollment(string registrationId, Attestation attestation)
        {
            Argument.AssertNotNullOrWhiteSpace(registrationId, nameof(registrationId));
            RegistrationId= registrationId;
            Attestation = GetAttestationMechanism(attestation);
        }

        /// <summary>
        /// Registration Id.
        /// </summary>
        /// <remarks>
        /// A valid registration Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided string does not fit the registration Id requirements.</exception>
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
        public AttestationMechanism Attestation { get; internal set; }

        /// <summary>
        /// Desired IoT hub to assign the device to.
        /// </summary>
        [JsonPropertyName("iotHubHostName")]
        public string IotHubHostName { get; set; }

        /// <summary>
        /// Initial twin state.
        /// </summary>
        [JsonPropertyName("initialTwin")]
        public InitialTwinState InitialTwinState { get; set; }

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
        public ProvisioningTwinCapabilities Capabilities { get; set; }

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

        private static AttestationMechanism GetAttestationMechanism(Attestation attestation)
        {
            Argument.AssertNotNull(attestation, nameof(attestation));

            var attestationMechanism = new AttestationMechanism();

            switch (attestation)
            {
                case X509Attestation:
                    attestationMechanism.Type = AttestationMechanismType.X509;
                    attestationMechanism.X509 = (X509Attestation)attestation;
                    break;


                case SymmetricKeyAttestation:
                    attestationMechanism.Type = AttestationMechanismType.SymmetricKey;
                    attestationMechanism.SymmetricKey = (SymmetricKeyAttestation)attestation;
                    break;

                case TpmAttestation:
                    attestationMechanism.Type = AttestationMechanismType.Tpm;
                    attestationMechanism.Tpm = (TpmAttestation)attestation;
                    break;

                default:
                    throw new ArgumentException("Unknown attestation mechanism", nameof(attestation));
            }

            return attestationMechanism;
        }
    }
}
