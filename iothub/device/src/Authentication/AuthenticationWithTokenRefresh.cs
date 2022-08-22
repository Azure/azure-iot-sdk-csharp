// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh.
    /// </summary>
    public abstract class AuthenticationWithTokenRefresh : IAuthenticationMethod, IDisposable
    {
        private const int DefaultSasRenewalBufferPercentage = 15;
        private static readonly TimeSpan s_defaultSasTimeToLive = TimeSpan.FromHours(1);

        private readonly TimeSpan _suggestedTimeToLive;
        private readonly int _timeBufferPercentage;

        private int _bufferSeconds;
        private SemaphoreSlim _lock = new(1);
        private string _token;
        private bool _isDisposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuthenticationWithTokenRefresh"/> class.
        /// </summary>
        /// <remarks>
        /// This constructor will create an authentication method instance that will be disposed when its
        /// associated device/ module client instance is disposed. To reuse the authentication method instance across
        /// multiple client instance lifetimes set <paramref name="disposeWithClient"/> to <c>false</c>.
        /// </remarks>
        /// <param name="suggestedTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="timeBufferPercentage">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has <see cref="DefaultSasRenewalBufferPercentage"/> percent or less of its lifespan left.
        ///</param>
        /// <param name="disposeWithClient "><c>true</c> if the authentication method should be disposed of by the client
        /// when the client using this instance is itself disposed; <c>false</c> if you intend to reuse the authentication method.
        /// Defaults to <c>true</c>.</param>
        public AuthenticationWithTokenRefresh(
            TimeSpan suggestedTimeToLive = default,
            int timeBufferPercentage = default,
            bool disposeWithClient = true)
        {
            if (suggestedTimeToLive.Ticks < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(suggestedTimeToLive));
            }

            if (timeBufferPercentage < 0 || timeBufferPercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(timeBufferPercentage));
            }

            _suggestedTimeToLive = suggestedTimeToLive == default
                ? s_defaultSasTimeToLive
                : suggestedTimeToLive;

            _timeBufferPercentage = timeBufferPercentage == default
                ? DefaultSasRenewalBufferPercentage
                : timeBufferPercentage;

            ExpiresOn = DateTime.UtcNow.AddSeconds(-_suggestedTimeToLive.TotalSeconds);
            Debug.Assert(IsExpiring);
            UpdateTimeBufferSeconds(_suggestedTimeToLive.TotalSeconds);

            DisposalWithClient = disposeWithClient;
        }

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

        // This internal property is used by the sdk to determine if the authentication method
        // should be disposed when the client that it is initialized with is disposed.
        internal bool DisposalWithClient { get; }

        /// <summary>
        /// Gets a snapshot of the security token associated with the device. This call is thread-safe.
        /// </summary>
        public async Task<string> GetTokenAsync(string iotHub)
        {
            if (_isDisposed)
            {
                if (Logging.IsEnabled)
                {
                    Logging.Error(
                        this,
                        $"Encountered {nameof(ObjectDisposedException)} - The authentication method instance has already been disposed, so this client is no longer usable.",
                        nameof(GetTokenAsync));
                }

                throw new ObjectDisposedException(GetType().Name, "The authentication method instance has already been disposed, so this client is no longer usable. " +
                    "Please close and dispose your current client instance. To continue carrying out operations from your device/ module, " +
                    "create a new authentication method instance and use it for reinitializing your client.");
            }

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

                _token = await SafeCreateNewToken(iotHub, _suggestedTimeToLive).ConfigureAwait(false);

                SharedAccessSignature sas = SharedAccessSignatureParser.Parse(_token);
                ExpiresOn = sas.ExpiresOn;
                UpdateTimeBufferSeconds((int)(ExpiresOn - DateTime.UtcNow).TotalSeconds);

                if (Logging.IsEnabled)
                    Logging.GenerateToken(this, ExpiresOn);

                return _token;
            }
            finally
            {
                _lock.Release();
            }
        }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionCredentials"/> instance based on a snapshot of the properties of
        /// the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionCredentials"/> instance.</returns>
        public virtual IotHubConnectionCredentials Populate(IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            if (iotHubConnectionCredentials == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionCredentials));
            }

            iotHubConnectionCredentials.SharedAccessSignature = _token;
            iotHubConnectionCredentials.SharedAccessKey = null;
            iotHubConnectionCredentials.SharedAccessKeyName = null;
            iotHubConnectionCredentials.SasTokenTimeToLive = _suggestedTimeToLive;
            iotHubConnectionCredentials.SasTokenRenewalBuffer = _timeBufferPercentage;

            return iotHubConnectionCredentials;
        }

        /// <summary>
        /// Creates a new token with a suggested TTL. This method is thread-safe.
        /// </summary>
        /// <param name="iotHub">The IoT hub domain name.</param>
        /// <param name="suggestedTimeToLive">The suggested TTL.</param>
        /// <returns>The token string.</returns>
        /// <remarks>This is an asynchronous method and should be awaited.</remarks>
        protected abstract Task<string> SafeCreateNewToken(string iotHub, TimeSpan suggestedTimeToLive);

        private void UpdateTimeBufferSeconds(double ttl)
        {
            _bufferSeconds = (int)(ttl * ((float)_timeBufferPercentage / 100));
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Component and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"Disposed={_isDisposed}; disposing={disposing}", $"{nameof(AuthenticationWithTokenRefresh)}.{nameof(Dispose)}");
                }

                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        _lock?.Dispose();
                        _lock = null;
                    }

                    _isDisposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposed={_isDisposed}; disposing={disposing}", $"{nameof(AuthenticationWithTokenRefresh)}.{nameof(Dispose)}");
                }
            }
        }

        /// <summary>
        /// Dispose resources
        /// </summary>
        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}
