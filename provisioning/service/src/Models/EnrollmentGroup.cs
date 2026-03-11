// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service enrollment group with a JSON serializer and deserializer.
    /// </summary>
    /// <remarks>
    /// This object is used to send EnrollmentGroup information to the provisioning service, or receive EnrollmentGroup
    /// information from the provisioning service.
    /// <para>
    /// To create or update an EnrollmentGroup on the provisioning service you should fill this object and call
    /// <see cref="EnrollmentGroupsClient.CreateOrUpdateAsync(EnrollmentGroup, System.Threading.CancellationToken)"/>.
    /// The minimum information required by the provisioning service is the <see cref="EnrollmentGroup.Id"/>
    /// and <see cref="EnrollmentGroup.Attestation"/>.
    /// </para>
    /// <para>
    /// To provision a device using an enrollment group, it must contain an X509 chip with a signing certificate for the
    /// <see cref="X509Attestation"/> mechanism.
    /// </para>
    /// <para>
    /// The content of this class will be serialized in a JSON format and sent as a body of the rest API to the
    /// provisioning service.
    /// </para>
    /// <para>
    /// The content of this class can be filled by a JSON, received from the provisioning service, as result of a
    /// enrollment group operation like create, update, or query for enrollment groups.
    /// </para>
    /// </remarks>
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
        /// </remarks>
        /// <param name="enrollmentGroupId">The string that uniquely identify this enrollmentGroup in the provisioning
        /// service. It cannot be null or empty.</param>
        /// <param name="attestation">The <see cref="Attestation"/> object with the attestation mechanism.</param>
        /// <exception cref="ArgumentNullException">If the provided <paramref name="enrollmentGroupId"/> is null.</exception>
        /// <exception cref="ArgumentException">If the provided <paramref name="enrollmentGroupId"/> is empty or white space.</exception>
        public EnrollmentGroup(string enrollmentGroupId, Attestation attestation)
        {
            Argument.AssertNotNullOrWhiteSpace(enrollmentGroupId, nameof(enrollmentGroupId));
            Id = enrollmentGroupId;
            Attestation = attestation;
        }

        // This JsonConstructor is used for serialization instead of the usual empty constructor
        // because one of this object's fields (attestation) doesn't map 1:1 with where that field
        // is in the JSON the service sends.
        [JsonConstructor]
        internal EnrollmentGroup(
            string enrollmentGroupId,
            AttestationMechanism attestation,
            string iotHubHostName,
            InitialTwin initialTwinState,
            ProvisioningStatus? provisioningStatus,
            DateTimeOffset createdOnUtc,
            DateTimeOffset lastUpdatedOnUtc,
            ETag eTag,
            InitialTwinCapabilities capabilities)
        {
            Argument.AssertNotNull(attestation, nameof(attestation));

            Id = enrollmentGroupId;
            // This is the one reason why we can't use an empty constructor here.
            Attestation = attestation.GetAttestation();
            IotHubHostName = iotHubHostName;
            InitialTwinState = initialTwinState;
            ProvisioningStatus = provisioningStatus;
            CreatedOnUtc = createdOnUtc;
            LastUpdatedOnUtc = lastUpdatedOnUtc;
            ETag = eTag;
            Capabilities = capabilities;
        }

        /// <summary>
        /// Enrollment group Id.
        /// </summary>
        /// <remarks>
        /// A valid enrollmentGroup Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided string does not fit the enrollmentGroup Id requirements</exception>
        [JsonProperty("enrollmentGroupId")]
        public string Id { get; internal set; }

        /// <summary>
        /// Current registration state.
        /// </summary>
        [JsonProperty("registrationState", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceRegistrationState RegistrationState { get; internal set; }

        /// <summary>
        /// Attestation mechanism.
        /// </summary>
        [JsonProperty("attestation")]
        private AttestationMechanism _attestation;

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
                    throw new InvalidOperationException($"Value for {nameof(Attestation)} cannot be null.");
                }
                else if (value is not X509Attestation && value is not SymmetricKeyAttestation)
                {
                    throw new InvalidOperationException("Attestation for enrollmentGroup shall be X509 or symmetric key.");
                }

                if (value is X509Attestation attestation)
                {
                    if (attestation.RootCertificates == null && attestation.CaReferences == null)
                    {
                        throw new InvalidOperationException("Attestation mechanism does not contain a valid certificate,");
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
        [JsonProperty("iotHubHostName", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string IotHubHostName { get; set; }

        /// <summary>
        /// Initial twin state.
        /// </summary>
        [JsonProperty("initialTwin", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public InitialTwin InitialTwinState { get; set; }

        /// <summary>
        /// The provisioning status.
        /// </summary>
        [JsonProperty("provisioningStatus", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ProvisioningStatus? ProvisioningStatus { get; set; }

        /// <summary>
        /// The DateTime this resource was created.
        /// </summary>
        [JsonProperty("createdDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset? CreatedOnUtc { get; internal set; }

        /// <summary>
        /// The DateTime this resource was last updated.
        /// </summary>
        [JsonProperty("lastUpdatedDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTimeOffset? LastUpdatedOnUtc { get; internal set; }

        /// <summary>
        /// Enrollment's ETag.
        /// </summary>
        [JsonProperty("etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))]
        public ETag ETag { get; set; }

        /// <summary>
        /// Capabilities of the device.
        /// </summary>
        [JsonProperty("capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public InitialTwinCapabilities Capabilities { get; set; }

        /// <summary>
        /// The behavior when a device is re-provisioned to an IoT hub.
        /// </summary>
        [JsonProperty("reprovisionPolicy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ReprovisionPolicy ReprovisionPolicy { get; set; }

        /// <summary>
        /// The allocation policy of this resource. Overrides the tenant level allocation policy.
        /// </summary>
        [JsonProperty("allocationPolicy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AllocationPolicy? AllocationPolicy { get; set; }

        /// <summary>
        /// The list of names of IoT hubs the device(s) in this resource can be allocated to.
        /// </summary>
        /// <remarks>
        /// Must be a subset of tenant level list of IoT hubs.
        /// </remarks>
        [JsonProperty("iotHubs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<string> IotHubs { get; set; } = new List<string>();

        /// <summary>
        /// Custom allocation definition.
        /// </summary>
        [JsonProperty("customAllocationDefinition", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CustomAllocationDefinition CustomAllocationDefinition { get; set; }

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
