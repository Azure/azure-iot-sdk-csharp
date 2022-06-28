// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Authentication;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents a provisioning registration message.
    /// </summary>
    public class ProvisioningTransportRegisterMessage
    {
        private readonly ProductInfo _productInfo = new();

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The global device endpoint for this message.</param>
        /// <param name="idScope">The IDScope for this message.</param>
        /// <param name="authentication">The authentication provider used to authenticate the client.</param>
        public ProvisioningTransportRegisterMessage(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authentication)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            IdScope = idScope;
            Authentication = authentication;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The global device endpoint for this message.</param>
        /// <param name="idScope">The IDScope for this message.</param>
        /// <param name="authentication">The authentication provider used to authenticate the client.</param>
        /// <param name="payload">The custom JSON content.</param>
        public ProvisioningTransportRegisterMessage(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authentication,
            string payload)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            IdScope = idScope;
            Authentication = authentication;
            if (!string.IsNullOrEmpty(payload))
            {
                Payload = payload;
            }
        }

        /// <summary>
        /// The global device endpoint for this message.
        /// </summary>
        public string GlobalDeviceEndpoint { get; }

        /// <summary>
        /// The IDScope for this message.
        /// </summary>
        public string IdScope { get; }

        /// <summary>
        /// The authentication provider used to authenticate the client.
        /// </summary>
        public AuthenticationProvider Authentication { get; }

        /// <summary>
        /// The custom content.
        /// </summary>
        public string Payload { get; }

        /// <summary>
        /// The product information sent to the Provisioning service. The application can specify extra information.
        /// </summary>
        public string ProductInfo
        {
            get => _productInfo.ToString();
            set => _productInfo.Extra = value;
        }
    }
}
