// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Primary and Secondary CA reference.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 CA references. It can receive primary and secondary
    /// CA references, but only the primary is mandatory.
    ///
    /// Users must provide the CA reference as a <code>String</code>. This class will encapsulate both in a
    /// single <see cref="X509Attestation"/>.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of the result of this class.
    /// <code>
    /// {
    ///     "primary": "ValidCAReference-1",
    ///     "secondary": "validCAReference-2"
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="!:https://docs.microsoft.com/en-us/rest/api/iot-dps/deviceenrollment">Device Enrollment</seealso>
    public class X509CAReferences
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509 CA references using the provided CA references.
        ///
        /// The CA reference is a <code>String</code> with the name that you gave for your certificate.
        /// </remarks>
        ///
        /// <param name="primary">the <code>String</code> with the primary CA reference. It cannot be <code>null</code> or empty.</param>
        /// <param name="secondary">the <code>String</code> with the secondary CA reference. It can be <code>null</code> or empty.</param>
        /// <exception cref="ProvisioningServiceClientException">if the primary CA reference is <code>null</code> or empty.</exception>
        [JsonConstructor]
        internal X509CAReferences(string primary, string secondary = null)
        {
            /* SRS_X509_CAREFERENCE_21_001: [The constructor shall throw ArgumentException if the primary CA reference is null or empty.] */
            if(string.IsNullOrWhiteSpace(primary))
            {
                throw new ProvisioningServiceClientException("Primary CA reference cannot be null or empty");
            }
            /* SRS_X509_CAREFERENCE_21_002: [The constructor shall store the primary and secondary CA references.] */
            Primary = primary;
            Secondary = secondary;
        }

        /// <summary>
        /// Primary reference.
        /// </summary>
        [JsonProperty(PropertyName = "primary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Primary { get; private set; }

        /// <summary>
        /// Secondary reference.
        /// </summary>
        [JsonProperty(PropertyName = "secondary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Secondary { get; private set; }
    }
}
