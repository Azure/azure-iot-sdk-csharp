// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents a Provisioning onboard message.
    /// </summary>
    public class ProvisioningTransportOnboardRequest
    {
        private readonly ProductInfo _productInfo = new ProductInfo();

        /// <summary>
        /// The Global Device Endpoint for this message.
        /// </summary>
        public string GlobalDeviceEndpoint { get; private set; }

        /// <summary>
        /// The SecurityProvider used to authenticate the client.
        /// </summary>
        public SecurityProvider Security { get; private set; }

        /// <summary>
        /// The Arc for Servers public key.
        /// </summary>
        public string PublicKey { get; private set; }

        /// <summary>
        /// The Product Information sent to the Provisioning Service. The application can specify extra information.
        /// </summary>
        public string ProductInfo
        {
            get => _productInfo.ToString();
            set => _productInfo.Extra = value;
        }

        /// <summary>
        /// Creates a new instance of the ProvisioningTransportOnboardRequest class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The Global Device Endpoint for this message.</param>
        /// <param name="security">The SecurityProvider used to authenticate the client.</param>
        /// <param name="publicKey">The Arc for Servers public key.</param>
        public ProvisioningTransportOnboardRequest(
            string globalDeviceEndpoint,
            SecurityProvider security,
            string publicKey)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            Security = security;
            PublicKey = publicKey;
        }
    }
}
