// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses the device connection string to generate SAS tokens for authenticating with service.
    /// </summary>
    public sealed class ClientAuthenticationWithConnectionString : IAuthenticationMethod
    {
        private const int DefaultSasRenewalBufferPercentage = 15;
        private static readonly TimeSpan s_defaultSasTimeToLive = TimeSpan.FromHours(1);

        private readonly TimeSpan _suggestedTimeToLive;
        private readonly int _timeBufferPercentage;
        private readonly IotHubConnectionString _iotHubConnectionString;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="connectionString">The connection string containing the device Id, optional module Id, shared access key name
        /// and shared access key to be used for authenticating with IoT hub service.</param>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        ///</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="suggestedTimeToLive"/> is a negative timespan, or if
        /// <paramref name="timeBufferPercentage"/> is outside the range 0-100.</exception>
        public ClientAuthenticationWithConnectionString(
            string connectionString,
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default)
        {
            if (suggestedTimeToLive < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(suggestedTimeToLive), "The TTL value cannot be negative.");
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(timeBufferPercentage), "The time buffer percentage cannot be out of the range 0-100.");
            }

            _suggestedTimeToLive = suggestedTimeToLive == default
                ? s_defaultSasTimeToLive
                : suggestedTimeToLive;

            _timeBufferPercentage = timeBufferPercentage == default
                ? DefaultSasRenewalBufferPercentage
                : timeBufferPercentage;

            _iotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
        }

        /// <summary>
        /// Populates an <c>IotHubConnectionCredential</c> instance based on the properties of the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        public void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            iotHubConnectionCredentials.DeviceId = _iotHubConnectionString.DeviceId;
            iotHubConnectionCredentials.ModuleId = _iotHubConnectionString.ModuleId;
            iotHubConnectionCredentials.SharedAccessKeyName = _iotHubConnectionString.SharedAccessKeyName;
            iotHubConnectionCredentials.SharedAccessKey = _iotHubConnectionString.SharedAccessKey;
            iotHubConnectionCredentials.SasTokenTimeToLive = _suggestedTimeToLive;
            iotHubConnectionCredentials.SasTokenRenewalBuffer = _timeBufferPercentage;
        }
    }
}
