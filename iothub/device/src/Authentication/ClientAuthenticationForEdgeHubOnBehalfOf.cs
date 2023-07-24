// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Authentication
{
    /// <summary>
    /// Authentication method that generates shared access signature (SAS) token with refresh, based on a provided shared access key (SAK).
    /// Build for gateway scenarios when one client authenticates on behalf of the other only.
    /// </summary>
    public class ClientAuthenticationForEdgeHubOnBehalfOf  : ClientAuthenticationWithSharedAccessKeyRefresh
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="sharedAccessKey">Shared access key value.</param>
        /// <param name="parentDeviceId">Identifier of the parent device.</param>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="moduleId">Module identifier.</param>
        /// <param name="sasTokenTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="sasTokenRenewalBuffer">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        /// </param>        
        public ClientAuthenticationForEdgeHubOnBehalfOf (
        string sharedAccessKey,
            string parentDeviceId,
            string deviceId,
            string moduleId = null,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default)
            : base(
                sharedAccessKey,
                deviceId,
                moduleId,
                sasTokenTimeToLive,
                sasTokenRenewalBuffer)
        {
            ParentDeviceId = parentDeviceId;
        }

        /// <summary>
        /// Gets the shared access key name.
        /// </summary>
        public string ParentDeviceId { get; private set; }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            string sasToken = SharedAccessSignatureBuilder.GetDeviceToken(iotHub, ParentDeviceId, "$edgeHub", SharedAccessKey);
            return Task.FromResult(sasToken);
        }
    }
}
