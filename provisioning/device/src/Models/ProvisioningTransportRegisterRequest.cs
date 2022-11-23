// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents a provisioning registration request.
    /// </summary>
    internal class ProvisioningTransportRegisterRequest
    {
        internal ProvisioningTransportRegisterRequest(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authentication,
            RegistrationRequestPayload payload = null)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            IdScope = idScope;
            Authentication = authentication;
            Payload = payload;
        }

        /// <summary>
        /// The global device endpoint for this message.
        /// </summary>
        internal string GlobalDeviceEndpoint { get; }

        /// <summary>
        /// The IDScope for this message.
        /// </summary>
        internal string IdScope { get; }

        /// <summary>
        /// The authentication provider used to authenticate the client.
        /// </summary>
        internal AuthenticationProvider Authentication { get; }

        /// <summary>
        /// The custom content.
        /// </summary>
        internal RegistrationRequestPayload Payload { get; }
    }
}
