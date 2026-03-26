// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Used to specify the authentication mechanism used by a device.
    /// </summary>
    public sealed class AuthenticationMechanism
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthenticationMechanism()
        {
        }

        /// <summary>
        /// Gets or sets the symmetric key used for authentication.
        /// </summary>
        [JsonPropertyName("symmetricKey")]
        public SymmetricKey SymmetricKey { get; set; }

        /// <summary>
        /// Gets or sets the X509 client certificate thumbprint.
        /// </summary>
        [JsonPropertyName("x509Thumbprint")]
        public X509Thumbprint X509Thumbprint { get; set; }

        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        [JsonPropertyName("type")]
        public ClientAuthenticationType Type { get; set; }
    }
}
