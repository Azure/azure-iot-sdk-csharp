// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Diagnostics;

    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh. 
    /// </summary>
    public abstract class AuthenticationWithTokenRefresh : IAuthenticationMethod
    {
        private readonly int _suggestedTimeToLiveSeconds;
        private readonly int _timeBufferPercentage;

        private int _bufferSeconds;
        private SemaphoreSlim _lock = new SemaphoreSlim(1);
        private string _token = null;
        private DateTime _expiryTime;

        /// <summary>
        /// Gets a snapshot of the UTC token expiry time.
        /// </summary>
        public DateTime ExpiresOn => _expiryTime;

        /// <summary>
        /// Gets a snapshot of the UTC token refresh time.
        /// </summary>
        public DateTime RefreshesOn => _expiryTime.AddSeconds(-_bufferSeconds);

        /// <summary>
        /// Gets a snapshot expiry state.
        /// </summary>
        public bool IsExpiring => (_expiryTime - DateTime.UtcNow).TotalSeconds <= _bufferSeconds;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationWithTokenRefresh"/> class.
        /// </summary>
        /// <param name="suggestedTimeToLiveSeconds">Token time to live suggested value. The implementations of this abstract
        /// may choose to ignore this value.</param>
        /// <param name="timeBufferPercentage">Time buffer before expiry when the token should be renewed expressed as 
        /// a percentage of the time to live.</param>
        public AuthenticationWithTokenRefresh(
            int suggestedTimeToLiveSeconds, 
            int timeBufferPercentage)
        {
            if (suggestedTimeToLiveSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(suggestedTimeToLiveSeconds));
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(timeBufferPercentage));
            }

            _suggestedTimeToLiveSeconds = suggestedTimeToLiveSeconds;
            _timeBufferPercentage = timeBufferPercentage;
            _expiryTime = DateTime.UtcNow.AddSeconds(- _suggestedTimeToLiveSeconds);
            Debug.Assert(IsExpiring);
            UpdateTimeBufferSeconds(_suggestedTimeToLiveSeconds);
        }

        /// <summary>
        /// Gets a snapshot of the security token associated with the device. This call is thread-safe.
        /// </summary>
        public async Task<string> GetTokenAsync(string iotHub)
        {
            if (!IsExpiring)
            {
                return _token;
            }

            await _lock.WaitAsync().ConfigureAwait(false);

            try
            {
                if (!IsExpiring)
                {
                    return _token;
                }

                _token = await SafeCreateNewToken(iotHub, _suggestedTimeToLiveSeconds).ConfigureAwait(false);

                SharedAccessSignature sas = SharedAccessSignature.Parse(".", _token);
                _expiryTime = sas.ExpiresOn;
                UpdateTimeBufferSeconds((int)(_expiryTime - DateTime.UtcNow).TotalSeconds);

                return _token;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionStringBuilder"/> instance based on a snapshot of the properties of 
        /// the current instance.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionStringBuilder"/> instance.</returns>
        public virtual IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.SharedAccessSignature = _token;
            iotHubConnectionStringBuilder.SharedAccessKey = null;
            iotHubConnectionStringBuilder.SharedAccessKeyName = null;

            return iotHubConnectionStringBuilder;
        }

        protected abstract Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive);

        private void UpdateTimeBufferSeconds(int ttl)
        {
            _bufferSeconds = (int)(ttl * ((float)_timeBufferPercentage / 100));
        }
    }
}
