// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using Microsoft.Azure.Devices.Client.Extensions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Authentication method that uses a shared access signature token and allows for token refresh. 
    /// </summary>
    public abstract class DeviceAuthenticationWithTokenRefresh : IAuthenticationMethod
    {
        private string _deviceId;
        private readonly int _timeToLiveBufferSeconds;
        private SemaphoreSlim _lock = new SemaphoreSlim(1);
        private string _token;
        private DateTime _expiryTime;

        public string DeviceId 
        {
            get
            {
                return _deviceId;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceAuthenticationWithTokenRefresh"/> class.
        /// </summary>
        /// <param name="deviceId">Device Identifier.</param>
        public DeviceAuthenticationWithTokenRefresh(string deviceId, 
            int timeToLiveBufferSeconds = 60) 
        {
            if (deviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (timeToLiveBufferSeconds < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(timeToLiveBufferSeconds));
            }

            _deviceId = deviceId;
            _timeToLiveBufferSeconds = timeToLiveBufferSeconds;
        }

        /// <summary>
        /// Gets a snapshot of the security token associated with the device. This call is thread-safe.
        /// </summary>
        public async Task<string> GetTokenAsync(string iotHubName)
        {
            if (!IsExpiring)
            {
                return _token;
            }

            await _lock.WaitAsync();

            if (!IsExpiring)
            {
                return _token;
            }

            _token = await SafeCreateNewToken(iotHubName);

            SharedAccessSignature sas = SharedAccessSignature.Parse(iotHubName, _token);
            _expiryTime = sas.ExpiresOn;

            _lock.Release();

            return _token;
        }

        /// <summary>
        /// Gets a snapshot of the UTC expiry token time.
        /// </summary>
        public DateTime ExpiresOn => _expiryTime;

        /// <summary>
        /// Gets a snapshot expiry state.
        /// </summary>
        public bool IsExpiring => (_expiryTime - DateTime.UtcNow).TotalSeconds <= _timeToLiveBufferSeconds;
        
        /// <summary>
        /// Populates an <see cref="IotHubConnectionStringBuilder"/> instance based on a snapshot of the properties of 
        /// the current instance.
        /// </summary>
        /// <param name="iotHubConnectionStringBuilder">Instance to populate.</param>
        /// <returns>The populated <see cref="IotHubConnectionStringBuilder"/> instance.</returns>
        public IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
        {
            if (iotHubConnectionStringBuilder == null)
            {
                throw new ArgumentNullException(nameof(iotHubConnectionStringBuilder));
            }

            iotHubConnectionStringBuilder.DeviceId = _deviceId;
            iotHubConnectionStringBuilder.SharedAccessSignature = _token;
            iotHubConnectionStringBuilder.SharedAccessKey = null;
            iotHubConnectionStringBuilder.SharedAccessKeyName = null;

            return iotHubConnectionStringBuilder;
        }

        protected abstract Task<string> SafeCreateNewToken(string iotHubName);
    }
}
