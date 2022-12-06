// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using System.ComponentModel;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Used to specify the authentication mechanism used by a device.
    /// </summary>
    public sealed class AuthenticationMechanism
    {
        private SymmetricKey _symmetricKey;
        private X509Thumbprint _x509Thumbprint;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public AuthenticationMechanism()
        {
            SymmetricKey = new SymmetricKey();
            X509Thumbprint = new X509Thumbprint();
            Type = ClientAuthenticationType.Sas;
        }

        /// <summary>
        /// Gets or sets the symmetric key used for authentication.
        /// </summary>
        [JsonProperty("symmetricKey")]
        public SymmetricKey SymmetricKey
        {
            get => _symmetricKey;
            set
            {
                _symmetricKey = value;
                if (value != null)
                {
                    Type = ClientAuthenticationType.Sas;
                }
            }
        }

        /// <summary>
        /// Gets or sets the X509 client certificate thumbprint.
        /// </summary>
        [JsonProperty("x509Thumbprint")]
        public X509Thumbprint X509Thumbprint
        {
            get => _x509Thumbprint;
            set
            {
                _x509Thumbprint = value;
                if (value != null)
                {
                    Type = ClientAuthenticationType.SelfSigned;
                }
            }
        }

        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        [DefaultValue(ClientAuthenticationType.Sas)]
        [JsonProperty("type", DefaultValueHandling = DefaultValueHandling.Populate)]
        public ClientAuthenticationType Type { get; set; }
    }
}
