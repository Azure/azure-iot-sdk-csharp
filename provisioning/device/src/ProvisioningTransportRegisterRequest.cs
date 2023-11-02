// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents a Provisioning registration message.
    /// </summary>
    public class ProvisioningTransportRegisterMessage : ProvisioningTransportRequest
    {
        /// <summary>
        /// The IDScope for this message.
        /// </summary>
        public string IdScope { get; private set; }

        /// <summary>
        /// The custom content.
        /// </summary>
        public string Payload { get; private set; }

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
            : base(globalDeviceEndpoint, security)
        {
            IdScope = idScope;
        }

        /// <summary>
        /// Creates a new instance of the ProvisioningTransportRegisterMessage class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The Global Device Endpoint for this message.</param>
        /// <param name="idScope">The IDScope for this message.</param>
        /// <param name="security">The SecurityProvider used to authenticate the client.</param>
        /// <param name="payload">The custom Json content.</param>
        public ProvisioningTransportRegisterMessage(
            string globalDeviceEndpoint,
            string idScope,
            SecurityProvider security,
            string payload)
            : base(globalDeviceEndpoint, security)
        {
            IdScope = idScope;
            if (!string.IsNullOrEmpty(payload))
            {
                Payload = payload;
            }
        }
    }
}
