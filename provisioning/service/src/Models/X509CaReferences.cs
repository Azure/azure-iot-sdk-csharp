// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

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
        /// Primary reference.
        /// </summary>
        [JsonPropertyName("primary")]
        public string Primary { get; private set; }

        /// <summary>
        /// Secondary reference.
        /// </summary>
        [JsonPropertyName("secondary")]
        public string Secondary { get; private set; }
    }
}
