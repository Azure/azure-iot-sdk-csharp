// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents a Provisioning issue challenge message.
    /// </summary>
    public class ProvisioningTransportIssueChallengeRequest : ProvisioningTransportRequest
    {
        /// <summary>
        /// The Arc for Servers public key.
        /// </summary>
        public string RegistrationId { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string EndorsementKey { get; private set; }
        /// <summary>
        /// 
        /// </summary>
        public string StorageRootKey { get; private set; }

        /// <summary>
        /// Creates a new instance of the ProvisioningTransportIssueChallengeRequest class.
        /// </summary>
        /// <param name="globalDeviceEndpoint">The Global Device Endpoint for this message.</param>
        /// <param name="security">The SecurityProvider used to authenticate the client.</param>
        /// <param name="registrationId"></param>
        /// <param name="endorsementKey"></param>
        /// <param name="storageRootKey"></param>
        public ProvisioningTransportIssueChallengeRequest(
            string globalDeviceEndpoint,
            SecurityProvider security,
            string registrationId,
            string endorsementKey,
            string storageRootKey)
            : base(globalDeviceEndpoint, security)
        {
            RegistrationId = registrationId;
            EndorsementKey = endorsementKey;
            StorageRootKey = storageRootKey;
        }
    }
}
