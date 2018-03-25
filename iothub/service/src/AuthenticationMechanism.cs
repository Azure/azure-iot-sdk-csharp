// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using Newtonsoft.Json;
    using System.ComponentModel;

    /// <summary>
    /// Used to specify the authentication mechanism used by a device.
    /// </summary>
    public sealed class AuthenticationMechanism
    {
        private SymmetricKey symmetricKey;
        private X509Thumbprint x509Thumbprint;

        /// <summary>
        /// default ctor
        /// </summary>
        public AuthenticationMechanism()
        {
            this.SymmetricKey = new SymmetricKey();
            this.X509Thumbprint = new X509Thumbprint();
            this.Type = AuthenticationType.Sas;
        }

        /// <summary>
        /// Gets or sets the <see cref="SymmetricKey"/> used for Authentication
        /// </summary>
        [JsonProperty(PropertyName = "symmetricKey")]
        public SymmetricKey SymmetricKey
        {
            get { return this.symmetricKey; }
            set
            {
                this.symmetricKey = value;
                if (value != null)
                {
                    this.Type = AuthenticationType.Sas;
                }
            }
        }

        /// <summary>
        /// Gets or sets the X509 client certificate thumbprint.
        /// </summary>
        [JsonProperty(PropertyName = "x509Thumbprint")]
        public X509Thumbprint X509Thumbprint
        {
            get { return this.x509Thumbprint; }
            set
            {
                this.x509Thumbprint = value;
                if (value != null)
                {
                    this.Type = AuthenticationType.SelfSigned;
                }
            }
        }

        /// <summary>
        /// Gets or sets the authentication type.
        /// </summary>
        [DefaultValue(AuthenticationType.Sas)]
        [JsonProperty(PropertyName = "type", DefaultValueHandling = DefaultValueHandling.Populate)]
        public AuthenticationType Type { get; set; }
    }
}
