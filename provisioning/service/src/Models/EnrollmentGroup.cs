// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using Azure;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service enrollment group with a JSON serializer and deserializer.
    /// </summary>
    /// <remarks>
    /// This object is used to send EnrollmentGroup information to the provisioning service, or receive EnrollmentGroup
    ///    information from the provisioning service.
    ///
    /// To create or update an EnrollmentGroup on the provisioning service you should fill this object and call the
    ///    public API {@link ProvisioningServiceClient#createOrUpdateEnrollmentGroup(EnrollmentGroup)}.
    ///    The minimum information required by the provisioning service is the {@link #enrollmentGroupId} and the
    ///    {@link #attestation}.
    ///
    /// To provision a device using EnrollmentGroup, it must contain a X509 chip with a signingCertificate for the
    ///    {@link X509Attestation} mechanism.
    ///
    /// The content of this class will be serialized in a JSON format and sent as a body of the rest API to the
    ///    provisioning service.
    ///
    /// The content of this class can be filled by a JSON, received from the provisioning service, as result of a
    ///    EnrollmentGroup operation like create, update, or query EnrollmentGroup.
    /// </remarks>
    /// <example>
    /// When serialized, an EnrollmentGroup will look like the following example:
    /// <code language="json">
    /// {
    ///    "enrollmentGroupId": "validEnrollmentGroupId",
    ///    "attestation": {
    ///        "type": "x509",
    ///        "signingCertificates": {
    ///            "primary": {
    ///                "certificate": "[valid certificate]"
    ///            }
    ///        }
    ///    },
    ///    "iotHubHostName": "ContosoIoTHub.azure-devices.net",
    ///    "provisioningStatus": "enabled"
    /// }
    /// </code>
    ///
    /// The following JSON is a sample of the EnrollmentGroup response, received from the provisioning service.
    /// <code>
    /// {
    ///    "enrollmentGroupId":"validEnrollmentGroupId",
    ///    "attestation":{
    ///        "type":"x509",
    ///        "signingCertificates":{
    ///            "primary":{
    ///                "certificate":"[valid certificate]",
    ///                "info": {
    ///                    "subjectName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///                    "sha1Thumbprint": "0000000000000000000000000000000000",
    ///                    "sha256Thumbprint": "validEnrollmentGroupId",
    ///                    "issuerName": "CN=ROOT_00000000-0000-0000-0000-000000000000, OU=Azure IoT, O=MSFT, C=US",
    ///                    "notBeforeUtc": "2017-11-14T12:34:18Z",
    ///                    "notAfterUtc": "2017-11-20T12:34:18Z",
    ///                    "serialNumber": "000000000000000000",
    ///                    "version": 3
    ///                }
    ///            }
    ///        }
    ///    },
    ///    "iotHubHostName":"ContosoIoTHub.azure-devices.net",
    ///    "provisioningStatus":"enabled",
    ///    "createdDateTimeUtc": "2017-09-28T16:29:42.3447817Z",
    ///    "lastUpdatedDateTimeUtc": "2017-09-28T16:29:42.3447817Z",
    ///    "etag": "\"00000000-0000-0000-0000-00000000000\""
    /// }
    /// </code>
    /// </example>
    public class EnrollmentGroup
    {
        /// <summary>
        /// Creates a new instance of EnrollmentGroup.
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the EnrollmentGroup object with the minimum set of
        /// information required by the provisioning service. A valid EnrollmentGroup must contain the
        /// enrollmentGroupId, which uniquely identify this enrollmentGroup, and the attestation mechanism,
        /// which must X509.
        ///
        /// Other parameters can be added by calling the setters on this object.
        /// </remarks>
        /// <example>
        /// When serialized, an EnrollmentGroup will look like the following example:
        /// <code language="json">
        /// {
        ///     "enrollmentGroupId": "validEnrollmentGroupId",
        ///     "attestation": {
        ///         "type": "x509",
        ///         "signingCertificates": {
        ///             "primary": {
        ///                 "certificate": "[valid certificate]"
        ///             }
        ///         }
        ///     }
        /// }
        /// </code>
        /// </example>
        /// <param name="enrollmentGroupId">The string that uniquely identify this enrollmentGroup in the provisioning
        /// service. It cannot be null or empty.</param>
        /// <param name="attestation">The <see cref="Attestation"/> object with the attestation mechanism.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
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
        public string EnrollmentGroupId { get; internal set; }

        /// <summary>
        /// Current registration state.
        /// </summary>
        [JsonPropertyName("registrationState")]
        public DeviceRegistrationState RegistrationState { get; internal set; }

        /// <summary>
        /// Attestation mechanism.
        /// </summary>
        [JsonPropertyName("attestation")]
        public AttestationMechanism Attestation { get; set; }

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The string with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonSerializer.Serialize(
                this,
                new JsonSerializerOptions
                {
                    WriteIndented = true,
                });
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
        /// The allocation policy of this resource. Overrides the tenant level allocation policy.
        /// </summary>
        [JsonPropertyName("allocationPolicy")]
        public AllocationPolicy? AllocationPolicy { get; set; }

        /// <summary>
        /// The list of names of IoT hubs the device(s) in this resource can be allocated to. Must be a subset of tenant level list of IoT hubs
        /// </summary>
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

            if (attestation is X509Attestation x509Attestation)
            {
                return new AttestationMechanism
                {
                    Type = AttestationMechanismType.X509,
                    X509 = x509Attestation,
                };
            }
            else if (attestation is TpmAttestation tpmAttestation)
            {
                return new AttestationMechanism
                {
                    Type = AttestationMechanismType.Tpm,
                    Tpm = tpmAttestation,
                };
            }
            else if (attestation is SymmetricKeyAttestation symmetricKeyAttestation)
            {
                return new AttestationMechanism
                {
                    Type = AttestationMechanismType.SymmetricKey,
                    SymmetricKey = symmetricKeyAttestation,
                };
            }
            else
            {
                throw new ArgumentException("Unknown attestation mechanism", nameof(attestation));
            }
        }
    }
}
