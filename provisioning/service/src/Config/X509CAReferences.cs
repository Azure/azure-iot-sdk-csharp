// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 Primary and Secondary CA reference.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 CA references. It can receive primary and secondary
    /// CA references.
    ///
    /// Users must provide the CA reference as a <c>String</c>. This class will encapsulate both in a
    /// single <see cref="X509Attestation"/>.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of the result of this class.
    /// <c>
    /// {
    ///     "primary": "ValidCAReference-1",
    ///     "secondary": "validCAReference-2"
    /// }
    /// </c>
    /// </example>
    public class X509CAReferences
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509 CA references using the provided CA references.
        ///
        /// The CA reference is a <c>String</c> with the name that you gave for your certificate.
        /// </remarks>
        /// <param name="primary">the <c>String</c> with the primary CA reference.</param>
        /// <param name="secondary">the <c>String</c> with the secondary CA reference.</param>
        [JsonConstructor]
        internal X509CAReferences(string primary, string secondary = null)
        {
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
