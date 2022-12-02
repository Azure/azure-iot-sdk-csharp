// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.Json.Serialization;
using System.Threading;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service enrollment group with a JSON serializer and deserializer.
    /// </summary>
    /// <remarks>
    /// This object is used to send EnrollmentGroup information to the provisioning service, or receive EnrollmentGroup
    /// information from the provisioning service.
    /// <para>
    /// To create or update an EnrollmentGroup on the provisioning service you should fill this object and call the
    /// public API <see cref="EnrollmentGroupsClient.CreateOrUpdateAsync(EnrollmentGroup, CancellationToken)"/>
    /// The minimum information required by the provisioning service is the <see cref="EnrollmentGroupId"/>
    /// and <see cref="Attestation"/>.
    /// </para>
    /// <para>
    /// To provision a device using EnrollmentGroup, it must contain a X509 chip with a signingCertificate for the
    /// <see cref="X509Attestation"/> mechanism.
    /// </para>
    /// </remarks>
    public class EnrollmentGroup
    {
        /// <summary>
        /// For deserialization.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public EnrollmentGroup()
        { }

        /// <summary>
        /// Creates an instance of this class with the required properties.
        /// </summary>
        /// <param name="enrollmentGroupId">The Id of the enrollment group.</param>
        /// <param name="attestation">The attestation.</param>
        /// <exception cref="ArgumentException">When <paramref name="attestation"/> is an unsupported type.</exception>
        public EnrollmentGroup(string enrollmentGroupId, Attestation attestation)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));
            EnrollmentGroupId = enrollmentGroupId;
            Attestation = GetAttestationMechanism(attestation);
        }

        /// <summary>
        /// Enrollment Group Id.
        /// </summary>
        /// <remarks>
        /// A valid enrollmentGroup Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided string does not fit the enrollmentGroup Id requirements</exception>
        [JsonPropertyName("enrollmentGroupId")]
        [JsonInclude]
        public string EnrollmentGroupId { get; internal set; }

        /// <summary>
        /// Current registration state.
        /// </summary>
        [JsonPropertyName("registrationState")]
        [JsonInclude]
        public DeviceRegistrationState RegistrationState { get; internal set; }

        /// <summary>
        /// Attestation mechanism.
        /// </summary>
        [JsonPropertyName("attestation")]
        [JsonInclude]
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
        [JsonInclude]
        public DateTimeOffset? CreatedOnUtc { get; internal set; }

        /// <summary>
        /// The DateTime this resource was last updated.
        /// </summary>
        [JsonPropertyName("lastUpdatedDateTimeUtc")]
        [JsonInclude]
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
        /// The allocation policy of this resource. Overrides the tenant level allocation policy.
        /// </summary>
        [JsonPropertyName("allocationPolicy")]
        public AllocationPolicy? AllocationPolicy { get; set; }

        /// <summary>
        /// The list of names of IoT hubs the device(s) in this resource can be allocated to.
        /// </summary>
        /// <remarks>
        /// Must be a subset of tenant level list of IoT hubs.
        /// </remarks>
        [JsonPropertyName("iotHubs")]
        public IList<string> IotHubs { get; set; } = new List<string>();

        /// <summary>
        /// Custom allocation definition.
        /// </summary>
        [JsonPropertyName("customAllocationDefinition")]
        public CustomAllocationDefinition CustomAllocationDefinition { get; set; }

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
