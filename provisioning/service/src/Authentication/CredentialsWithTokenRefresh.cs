// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Authentication class implementing SAS token refresh
    /// </summary>
    public abstract class CredentialsWithTokenRefresh: ProvisioningServiceClientCredentials, IDisposable
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
        /// Initializes a new instance of the <see cref="CredentialsWithTokenRefresh"/> class.
        /// </summary>
        /// <param name="suggestedTimeToLiveSeconds">Token time to live suggested value. The implementations of this abstract
        /// may choose to ignore this value.</param>
        /// <param name="timeBufferPercentage">Time buffer before expiry when the token should be renewed expressed as 
        /// a percentage of the time to live.</param>
        public CredentialsWithTokenRefresh(
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
            _expiryTime = DateTime.UtcNow.AddSeconds(-_suggestedTimeToLiveSeconds);
            Debug.Assert(IsExpiring);
            UpdateTimeBufferSeconds(_suggestedTimeToLiveSeconds);
        }

        /// <summary>
        /// Gets a snapshot of the security token associated with the device. This call is thread-safe.
        /// </summary>
        public override async Task<string> GetSasToken()
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

                _token = await SafeCreateNewToken(_suggestedTimeToLiveSeconds).ConfigureAwait(false);

                SharedAccessSignature sas = SharedAccessSignature.Parse(".", _token);
                _expiryTime = sas.ExpiresOn;
                UpdateTimeBufferSeconds((int)(_expiryTime - DateTime.UtcNow).TotalSeconds);

                /* TODO: Add logging CaptureLogs - DPS Service
                if (Logging.IsEnabled) Logging.GenerateToken(this, _expiryTime);
                */

                return _token;
            }
            finally
            {
                _lock.Release();
            }

        }

        /// <summary>
        /// Creates a new token with a suggested TTL. This method is thread-safe.
        /// </summary>
        /// <param name="suggestedTimeToLive">The suggested TTL.</param>
        /// <returns>The token string.</returns>
        protected abstract Task<string> SafeCreateNewToken(int suggestedTimeToLive);

        private void UpdateTimeBufferSeconds(int ttl)
        {
            _bufferSeconds = (int)(ttl * ((float)_timeBufferPercentage / 100));
        }

        /// <summary>
        /// Releases the unmanaged resources used by the CredentialsWithTokenRefresh and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                // dispose managed resources
                _lock.Dispose();
            }
            // free native resources
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used by the invoker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
