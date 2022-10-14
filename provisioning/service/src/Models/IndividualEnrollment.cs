// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Azure;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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

        /// <summary>
        /// Constructor for serialization purposes only.
        /// </summary>
        internal IndividualEnrollment()
        {
        }

        /// <summary>
        /// Registration Id.
        /// </summary>
        /// <remarks>
        /// A valid registration Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <exception cref="InvalidOperationException">If the provided string does not fit the registration Id requirements</exception>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; internal set; }

        /// <summary>
        /// Desired IoT hub device Id (optional).
        /// </summary>
        [JsonProperty(PropertyName = "deviceId", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string DeviceId { get; set; }

        /// <summary>
        /// Current registration state.
        /// </summary>
        [JsonProperty(PropertyName = "registrationState", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceRegistrationState RegistrationState { get; internal set; }

        /// <summary>
        /// Attestation mechanism.
        /// </summary>
        [JsonProperty(PropertyName = "attestation")]
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
                        && attestation.CAReferences == null)
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
        public DateTime? CreatedDateTimeUtc { get; internal set; }

        /// <summary>
        /// The DateTime this resource was last updated.
        /// </summary>
        [JsonProperty(PropertyName = "lastUpdatedDateTimeUtc", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DateTime? LastUpdatedDateTimeUtc { get; internal set; }

        /// <summary>
        /// Enrollment's ETag.
        /// </summary>
        [JsonProperty(PropertyName = "etag", DefaultValueHandling = DefaultValueHandling.Ignore)]
        [JsonConverter(typeof(NewtonsoftJsonETagConverter))] // NewtonsoftJsonETagConverter is used here because otherwise the ETag isn't serialized properly
        public ETag ETag { get; set; }

        /// <summary>
        /// Capabilities of the device.
        /// </summary>
        [JsonProperty(PropertyName = "capabilities", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public DeviceCapabilities Capabilities { get; set; }

        /// <summary>
        /// The behavior when a device is re-provisioned to an IoT hub.
        /// </summary>
        [JsonProperty(PropertyName = "reprovisionPolicy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public ReprovisionPolicy ReprovisionPolicy { get; set; }

        /// <summary>
        /// Custom allocation definition.
        /// </summary>
        [JsonProperty(PropertyName = "customAllocationDefinition", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public CustomAllocationDefinition CustomAllocationDefinition { get; set; }

        /// <summary>
        /// The allocation policy of this resource. Overrides the tenant level allocation policy.
        /// </summary>
        [JsonProperty(PropertyName = "allocationPolicy", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public AllocationPolicy? AllocationPolicy { get; set; }

        /// <summary>
        /// The list of names of IoT hubs the device in this resource can be allocated to. Must be a subset of tenant level list of IoT hubs.
        /// </summary>
        [JsonProperty(PropertyName = "iotHubs", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public IList<string> IotHubs { get; set; } = new List<string>();

        /// <summary>
        /// Convert this object in a pretty print format.
        /// </summary>
        /// <returns>The string with the content of this class in a pretty print format.</returns>
        public override string ToString()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
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
