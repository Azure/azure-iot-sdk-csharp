// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public abstract class ClientAuthenticationWithTokenRefresh : IAuthenticationMethod
    {
        private const int DefaultSasRenewalBufferPercentage = 15;
        private static readonly TimeSpan s_defaultSasTimeToLive = TimeSpan.FromHours(1);
        private int _bufferSeconds;
        private readonly IotHubConnectionString _iotHubConnectionString;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="deviceId">Device identifier.</param>
        /// <param name="moduleId">Module identifier.</param>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has <see cref="DefaultSasRenewalBufferPercentage"/> percent or less of its lifespan left.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="deviceId"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="deviceId"/> or <paramref name="moduleId"/> is empty or whitespace.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="suggestedTimeToLive"/> is a negative timespan, or if <paramref name="timeBufferPercentage"/> is outside the range 0-100.
        /// </exception>
        public ClientAuthenticationWithTokenRefresh(
            string deviceId,
            string moduleId = default,
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default)
        {
            Argument.AssertNotNullOrWhiteSpace(deviceId, nameof(deviceId));
            DeviceId = deviceId;

            if (moduleId != null
                && moduleId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Module Id cannot be white space.");
            }

            ModuleId = moduleId;

            if (suggestedTimeToLive < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(suggestedTimeToLive));
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(timeBufferPercentage));
            }

            SuggestedTimeToLive = suggestedTimeToLive == default
                ? s_defaultSasTimeToLive
                : suggestedTimeToLive;

            TimeBufferPercentage = timeBufferPercentage == default
                ? DefaultSasRenewalBufferPercentage
                : timeBufferPercentage;

            ExpiresOn = DateTime.UtcNow.AddSeconds(-SuggestedTimeToLive.TotalSeconds);
            Debug.Assert(IsExpiring);
            UpdateTimeBufferSeconds(SuggestedTimeToLive.TotalSeconds);
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string containing the device Id, optional module Id, shared access key name
        /// and shared access key to be used for authenticating with IoT hub service.
        /// </param>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty or whitespace.</exception>
        /// <exception cref="ArgumentOutOfRangeException">
        /// Thrown if <paramref name="suggestedTimeToLive"/> is a negative timespan, or if <paramref name="timeBufferPercentage"/> is outside the range 0-100.
        /// </exception>
        public ClientAuthenticationWithTokenRefresh(
            string connectionString,
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default)
        {
            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _iotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
            DeviceId = _iotHubConnectionString.DeviceId;
            ModuleId = _iotHubConnectionString.ModuleId;

            if (suggestedTimeToLive < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(suggestedTimeToLive));
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(timeBufferPercentage));
            }

            SuggestedTimeToLive = suggestedTimeToLive == default
                ? s_defaultSasTimeToLive
                : suggestedTimeToLive;

            TimeBufferPercentage = timeBufferPercentage == default
                ? DefaultSasRenewalBufferPercentage
                : timeBufferPercentage;

            ExpiresOn = DateTime.UtcNow.AddSeconds(-SuggestedTimeToLive.TotalSeconds);
            Debug.Assert(IsExpiring);
            UpdateTimeBufferSeconds(SuggestedTimeToLive.TotalSeconds);
        }

        /// <summary>
        /// Gets the device Id.
        /// </summary>
        public string DeviceId { get; private set; }

        /// <summary>
        /// Gets the module Id.
        /// </summary>
        public string ModuleId { get; private set; }

        /// <summary>
        /// Gets a snapshot of the UTC token expiry time.
        /// </summary>
        public DateTime ExpiresOn { get; private set; }

        /// <summary>
        /// Gets a snapshot of the UTC token refresh time.
        /// </summary>
        public DateTime RefreshesOn => ExpiresOn.AddSeconds(-_bufferSeconds);

        /// <summary>
        /// Gets a snapshot expiry state.
        /// </summary>
        public bool IsExpiring => (ExpiresOn - DateTime.UtcNow).TotalSeconds <= _bufferSeconds;

        internal string Token { get; private set; }

        internal TimeSpan SuggestedTimeToLive { get; }

        internal int TimeBufferPercentage { get; }

        /// <summary>
        /// Gets a snapshot of the security token associated with the device.
        /// </summary>
        public async Task<string> GetTokenAsync(string iotHub)
        {
            // We realize that this code does not ensure a new token is not generated multiple times during this window,
            // if multiple calls are made very near each other. The cost of ensuring it means having a SemaphoreSlim
            // class member, which is IDisposable and requires that the class also be IDisposable. That causes
            // a bunch of downstream problems for customers that we think is not worth the trade off.
            // Also, over MQTT we only renew the token on connect (an expired token causes a disconnect) which is
            // already forced to be one thread doing the connecting. Over AMQP, we proactively renew the token, which
            // is also forced to be one thread renewing. As we've deprecated HTTP, we're left with no chance of this
            // happening, when used by a single client.

            if (!IsExpiring)
            {
                return Token;
            }

            string token = await SafeCreateNewTokenAsync(iotHub, SuggestedTimeToLive).ConfigureAwait(false);

            SharedAccessSignature sas = SharedAccessSignatureParser.Parse(token);

            Token = token;
            ExpiresOn = sas.ExpiresOn;

            UpdateTimeBufferSeconds((int)(ExpiresOn - DateTime.UtcNow).TotalSeconds);

            if (Logging.IsEnabled)
                Logging.GenerateToken(this, ExpiresOn);

            return Token;
        }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionCredentials"/> instance based on a snapshot of the properties of
        /// the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="iotHubConnectionCredentials"/> is null.</exception>
        public virtual void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.SharedAccessSignature = Token;
            iotHubConnectionCredentials.SharedAccessKey = null;
            iotHubConnectionCredentials.SharedAccessKeyName = null;
            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
            iotHubConnectionCredentials.SasTokenTimeToLive = SuggestedTimeToLive;
            iotHubConnectionCredentials.SasTokenRenewalBuffer = TimeBufferPercentage;
        }

        /// <summary>
        /// Creates a new token with a suggested TTL.
        /// </summary>
        /// <param name="iotHub">The IoT hub domain name.</param>
        /// <param name="suggestedTimeToLive">The suggested TTL.</param>
        /// <returns>The token string.</returns>
        protected abstract Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive);

        private void UpdateTimeBufferSeconds(double ttl)
        {
            _bufferSeconds = (int)(ttl * ((float)TimeBufferPercentage / 100));
        }
    }
}
