// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents a provisioning registration request.
    /// </summary>
    internal sealed class ProvisioningTransportRegisterRequest
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The global device endpoint for this message.</param>
        /// <param name="idScope">The IDScope for this message.</param>
        /// <param name="authentication">The authentication provider used to authenticate the client.</param>
        /// <param name="payload">The custom JSON content.</param>
        public ProvisioningTransportRegisterRequest(
            string globalDeviceEndpoint,
            string idScope,
            AuthenticationProvider authentication,
            RegistrationRequestPayload payload = default)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            IdScope = idScope;
            Authentication = authentication;
            if (payload != default)
            {
                Payload = payload;
            }
        }

        // For unit testing purpose only.
        internal ProvisioningTransportRegisterRequest()
        { }

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
