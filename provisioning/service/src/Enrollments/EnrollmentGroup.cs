// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
    /// <c>
    /// {
    ///    "enrollmentGroupId":"validEnrollmentGroupId",
    ///    "attestation":{
    ///        "type":"x509",
    ///        "signingCertificates":{
    ///            "primary":{
    ///                "certificate":"[valid certificate]"
    ///            }
    ///        }
    ///    },
    ///    "iotHubHostName":"ContosoIoTHub.azure-devices.net",
    ///    "provisioningStatus":"enabled"
    /// }
    /// </c>
    ///
    /// The following JSON is a sample of the EnrollmentGroup response, received from the provisioning service.
    /// <c>
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
    /// </c>
    /// </example>
    public class EnrollmentGroup : IETagHolder
    {
        /// <summary>
        /// Creates a new instance of <c>EnrollmentGroup</c>.
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
        /// <c>
        /// {
        ///     "enrollmentGroupId":"validEnrollmentGroupId",
        ///     "attestation":{
        ///         "type":"x509",
        ///         "signingCertificates":{
        ///             "primary":{
        ///                 "certificate":"[valid certificate]"
        ///             }
        ///         }
        ///     }
        /// }
        /// </c>
        /// </example>
        /// <param name="enrollmentGroupId">the <c>string</c> that uniquely identify this enrollmentGroup in the provisioning
        ///     service. It cannot be <c>null</c> or empty.</param>
        /// <param name="attestation">the <see cref="Attestation"/> object with the attestation mechanism. It cannot be <c>null</c>.</param>
        /// <exception cref="ArgumentNullException">if one of the provided parameters is not correct</exception>
        public EnrollmentGroup(string enrollmentGroupId, Attestation attestation)
        {
            EnrollmentGroupId = enrollmentGroupId ?? throw new ArgumentNullException(nameof(enrollmentGroupId));
            Attestation = attestation ?? throw new ArgumentNullException(nameof(attestation));
        }

        /// <summary>
        /// Creates a new instance of <c>EnrollmentGroup</c> using information in a JSON.
        /// </summary>
        /// <remarks>
        /// This constructor creates an instance of the enrollmentGroup filling the class with the information
        /// provided in the JSON. It is used by the SDK to parse EnrollmentGroup responses from the provisioning service.
        /// </remarks>
        /// <example>
        /// The following JSON is a sample of the EnrollmentGroup response, received from the provisioning service.
        /// <c>
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
        /// </c>
        /// </example>
        /// <param name="enrollmentGroupId">the <c>string</c> with a unique id for the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="attestation">the <see cref="AttestationMechanism"/> for the enrollment. It shall be `X509` or `SymmetricKey`.</param>
        /// <param name="iotHubHostName">the <c>string</c> with the target IoTHub name. This is optional and can be <c>null</c> or empty.</param>
        /// <param name="initialTwinState">the <see cref="TwinState"/> with the initial Twin condition. This is optional and can be <c>null</c>.</param>
        /// <param name="provisioningStatus">the <see cref="ProvisioningStatus"/> that determine the initial status of the device. This is optional and can be <c>null</c>.</param>
        /// <param name="createdDateTimeUtc">the <c>DateTime</c> with the date and time that the enrollment was created. This is optional and can be <c>null</c>.</param>
        /// <param name="lastUpdatedDateTimeUtc">the <c>DateTime</c> with the date and time that the enrollment was updated. This is optional and can be <c>null</c>.</param>
        /// <param name="eTag">the <c>string</c> with the eTag that identify the correct instance of the enrollment in the service. It cannot be <c>null</c> or empty.</param>
        /// <param name="capabilities">The capabilities of the device (ie: is it an edge device?)</param>
        /// <exception cref="ProvisioningServiceClientException">if the received JSON is invalid.</exception>
        [JsonConstructor]
        internal EnrollmentGroup(
            string enrollmentGroupId,
            AttestationMechanism attestation,
            string iotHubHostName,
            TwinState initialTwinState,
            ProvisioningStatus? provisioningStatus,
            DateTime createdDateTimeUtc,
            DateTime lastUpdatedDateTimeUtc,
            string eTag,
            DeviceCapabilities capabilities)
        {
            if (attestation == null)
            {
                throw new ProvisioningServiceClientException("Service respond an enrollmentGroup without attestation.");
            }

            try
            {
                EnrollmentGroupId = enrollmentGroupId;
                Attestation = attestation.GetAttestation();
                IotHubHostName = iotHubHostName;
                InitialTwinState = initialTwinState;
                ProvisioningStatus = provisioningStatus;
                CreatedDateTimeUtc = createdDateTimeUtc;
                LastUpdatedDateTimeUtc = lastUpdatedDateTimeUtc;
                ETag = eTag;
                Capabilities = capabilities;
            }
            catch (ArgumentException e)
            {
                throw new ProvisioningServiceClientException(e);
            }
        }

        /// <summary>
        /// Enrollment Group Id.
        /// </summary>
        /// <remarks>
        /// A valid enrollmentGroup Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="ArgumentException">if the provided string does not fit the enrollmentGroup Id requirements</exception>
        [JsonProperty(PropertyName = "enrollmentGroupId")]
        public string EnrollmentGroupId { get; private set; }

        /// <summary>
        /// Current registration state.
        /// </summary>
        [JsonProperty(PropertyName = "registrationState", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceRegistrationState RegistrationState { get; private set; }

        /// <summary>
        /// Attestation Mechanism
        /// </summary>
        [JsonProperty(PropertyName = "attestation")]
        private AttestationMechanism _attestation;

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The <c>string</c> with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            string jsonPrettyPrint = JsonConvert.SerializeObject(this, Formatting.Indented);
            return jsonPrettyPrint;
        }

        /// <summary>
        /// Getter and setter for Attestation.
        /// </summary>
        [JsonIgnore]
        public Attestation Attestation
        {
            get => _attestation.GetAttestation();
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }
                else if (value is not X509Attestation && value is not SymmetricKeyAttestation)
                {
                    throw new ArgumentException("Attestation for enrollmentGroup shall be X509 or symmetric key");
                }

                if (value is X509Attestation attestation)
                {
                    if (attestation.RootCertificates == null && attestation.CAReferences == null)
                    {
                        throw new ArgumentException("Attestation mechanism does not contain a valid certificate");
                    }
                }

                _attestation = new AttestationMechanism(value);
            }
        }

        /// <summary>
        /// Desired IotHub to assign the device to
        /// </summary>
        [JsonProperty(PropertyName = "iotHubHostName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string IotHubHostName { get; set; }

        /// <summary>
        /// Initial twin state.
        /// </summary>
        [JsonProperty(PropertyName = "initialTwin", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public TwinState InitialTwinState { get; set; }

        /// <summary>
        /// The provisioning status.
        /// </summary>
        [JsonConverter(typeof(StringEnumConverter))]
        [JsonProperty(PropertyName = "provisioningStatus", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ProvisioningStatus? ProvisioningStatus { get; set; }

        /// <summary>
        /// The DateTime this resource was created.
        /// </summary>
        [JsonProperty(PropertyName = "createdDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? CreatedDateTimeUtc { get; private set; }

        /// <summary>
        /// The DateTime this resource was last updated.
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? LastUpdatedDateTimeUtc { get; private set; }

        /// <summary>
        /// Enrollment's ETag
        /// </summary>
        [JsonProperty(PropertyName = "etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string ETag { get; set; }

        /// <summary>
        /// Capabilities of the device
        /// </summary>
        [JsonProperty(PropertyName = "capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceCapabilities Capabilities { get; set; }

        /// <summary>
        /// The behavior when a device is re-provisioned to an IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "reprovisionPolicy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ReprovisionPolicy ReprovisionPolicy { get; set; }

        /// <summary>
        /// The allocation policy of this resource. Overrides the tenant level allocation policy.
        /// </summary>
        [JsonProperty(PropertyName = "allocationPolicy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AllocationPolicy? AllocationPolicy { get; set; }

        /// <summary>
        /// The list of names of IoT hubs the device(s) in this resource can be allocated to. Must be a subset of tenant level list of IoT hubs
        /// </summary>
        [JsonProperty(PropertyName = "iotHubs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<string> IotHubs { get; private set; } = new List<string>();

        /// <summary>
        /// Custom allocation definition.
        /// </summary>
        [JsonProperty(PropertyName = "customAllocationDefinition", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CustomAllocationDefinition CustomAllocationDefinition { get; set; }

        /// <summary>
        /// For use in serialization.
        /// </summary>
        /// <seealso href="https://www.newtonsoft.com/json/help/html/ConditionalProperties.htm#ShouldSerialize"/>
        public bool ShouldSerializeIotHubs()
        {
            return IotHubs.Any();
        }
    }
}
