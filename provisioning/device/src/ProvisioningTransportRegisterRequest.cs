// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents a Provisioning registration message.
    /// </summary>
    public class ProvisioningTransportRegisterMessage
    {
        private ProductInfo _productInfo = new ProductInfo();

        /// <summary>
        /// The Global Device Endpoint for this message.
        /// </summary>
        public string GlobalDeviceEndpoint { get; private set; }

        /// <summary>
        /// The IDScope for this message.
        /// </summary>
        public string IdScope { get; private set; }

        /// <summary>
        /// The SecurityProvider used to authenticate the client.
        /// </summary>
        public SecurityProvider Security { get; private set; }

        /// <summary>
        /// The Product Information sent to the Provisioning Service. The application can specify extra information.
        /// </summary>
        public string ProductInfo
        {
            get
            {
                return _productInfo.ToString();
            }
            set
            {
                _productInfo.Extra = value;
            }
        }

        /// <summary>
        /// Creates a new instance of the ProvisioningTransportRegisterMessage class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The Global Device Endpoint for this message.</param>
        /// <param name="idScope">The IDScope for this message.</param>
        /// <param name="security">The SecurityProvider used to authenticate the client.</param>
        public ProvisioningTransportRegisterMessage(
            string globalDeviceEndpoint,
            string idScope,
            SecurityProvider security)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            IdScope = idScope;
            Security = security;
        }
    }
}
