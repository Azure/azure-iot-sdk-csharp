// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public abstract class DeviceAuthenticationWithTokenRefresh : AuthenticationWithTokenRefresh
    {
        /// <summary>
        /// Initializes a new instance of the <c>DeviceAuthenticationWithTokenRefresh</c> class.
        /// </summary>
        /// <remarks>
        /// This constructor will create an authentication method instance that will be disposed when its
        /// associated device client instance is disposed. To reuse the authentication method instance across multiple client instance lifetimes
        /// set <paramref name="disposeWithClient"/> to <c>false</c>.
        /// </remarks>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="suggestedTimeToLiveSeconds">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        ///<param name="disposeWithClient ">
        ///<c>true</c> if the authentication method should be disposed of by the client
        /// when the client using this instance is itself disposed; <c>false</c> if you intend to reuse the authentication method.
        /// Defaults to <c>true</c>.
        /// </param>
        public DeviceAuthenticationWithTokenRefresh(
            string deviceId,
            int suggestedTimeToLiveSeconds = default,
            int timeBufferPercentage = default,
            bool disposeWithClient = true)
            : base(
                  SetSasTokenSuggestedTimeToLiveSeconds(suggestedTimeToLiveSeconds),
                  SetSasTokenRenewalBufferPercentage(timeBufferPercentage),
                  disposeWithClient)
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            DeviceId = deviceId;
        }

        /// <summary>
        /// Gets the DeviceId.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Populates a supplied instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionCredentials"/> instance.</returns>
        public override IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            iotHubConnectionCredentials = base.Populate(iotHubConnectionCredentials);
            iotHubConnectionCredentials.DeviceId = DeviceId;
            return iotHubConnectionCredentials;
        }

        private static int SetSasTokenSuggestedTimeToLiveSeconds(int suggestedTimeToLiveSeconds)
        {
            return (int)(suggestedTimeToLiveSeconds == 0
                ? SharedAccessSignatureConstants.DefaultSasTimeToLive.TotalSeconds
                : suggestedTimeToLiveSeconds);
        }

        private static int SetSasTokenRenewalBufferPercentage(int timeBufferPercentage)
        {
            return timeBufferPercentage == 0
                ? SharedAccessSignatureConstants.DefaultSasRenewalBufferPercentage
                : timeBufferPercentage;
        }
    }
}
