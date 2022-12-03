// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Representation of a single Device Provisioning Service X509 primary and secondary certificate authority references.
    /// </summary>
    /// <remarks>
    /// This class creates a representation of an X509 certificate authority references. It can receive primary and secondary
    /// CA references.
    ///
    /// Users must provide the certificate authority reference as a string. This class will encapsulate both in a
    /// single <see cref="X509Attestation"/>.
    /// </remarks>
    /// <example>
    /// The following JSON is an example of the result of this class.
    /// <code language="json">
    /// {
    ///     "primary": "ValidCAReference-1",
    ///     "secondary": "validCAReference-2"
    /// }
    /// </code>
    /// </example>
    public class X509CaReferences
    {
        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <remarks>
        /// Creates a new instance of the X509 CA references using the provided CA references.
        ///
        /// The CA reference is a String with the name that you gave for your certificate.
        /// </remarks>
        /// <param name="primary">the String with the primary CA reference.</param>
        /// <param name="secondary">the String with the secondary CA reference.</param>
        [JsonConstructor]
        protected internal X509CaReferences(string primary, string secondary = default)
        {
            Primary = primary;
            Secondary = secondary;
        }

        /// <summary>
        /// Primary reference.
        /// </summary>
        [JsonProperty("primary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Primary { get; private set; }

        /// <summary>
        /// Secondary reference.
        /// </summary>
        [JsonProperty("secondary", DefaultValueHandling = DefaultValueHandling.Ignore)]
        public string Secondary { get; private set; }
    }
}
