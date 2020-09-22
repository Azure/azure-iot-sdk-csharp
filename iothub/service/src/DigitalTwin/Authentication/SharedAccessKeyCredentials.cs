// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Common.Security;

namespace Microsoft.Azure.Devices.Authentication
{
    internal class SharedAccessKeyCredentials : IotServiceClientCredentials
    {
        // Time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        // The token will be renewed when it has 15% or less of the sas token's lifespan left.
        private const int RenewalTimeBufferPercentage = 15;

        private readonly object _sasLock = new object();
        private readonly TimeSpan _sasTokenTimeToLive = TimeSpan.FromMinutes(20);

        private readonly string _sharedAccessKey;
        private readonly string _sharedAccessPolicy;
        private readonly string _audience;

        private string _cachedSasToken;
        private DateTimeOffset _tokenExpiryTime;

        /// <summary>
        /// Initializes a new instance of <see cref="SharedAccessKeyCredentials"/> class.
        /// </summary>
        /// <param name="connectionString">The IoT Hub connection string.</param>
        internal SharedAccessKeyCredentials(string connectionString)
        {
            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);

            _sharedAccessKey = iotHubConnectionString.SharedAccessKey;
            _sharedAccessPolicy = iotHubConnectionString.SharedAccessKeyName;
            _audience = iotHubConnectionString.Audience;

            _cachedSasToken = null;
        }

        /// <inheritdoc />
        protected override string GetSasToken()
        {
            lock (_sasLock)
            {
                if (TokenShouldBeGenerated())
                {
                    var builder = new SharedAccessSignatureBuilder
                    {
                        KeyName = _sharedAccessPolicy,
                        Key = _sharedAccessKey,
                        TimeToLive = _sasTokenTimeToLive,
                        Target = _audience,
                    };

                    _tokenExpiryTime = DateTimeOffset.UtcNow.Add(builder.TimeToLive);
                    _cachedSasToken = builder.ToSignature();
                }

                return _cachedSasToken;
            }
        }

        private bool TokenShouldBeGenerated()
        {
            // The token needs to be generated if this is the first time it is being accessed (not cached yet)
            // or the current time is greater than or equal to the token expiry time, less 15% buffer.
            if (_cachedSasToken == null)
            {
                return true;
            }

            double bufferTimeInMilliseconds = (double)RenewalTimeBufferPercentage / 100 * _sasTokenTimeToLive.TotalMilliseconds;
            DateTimeOffset tokenExpiryTimeWithBuffer = _tokenExpiryTime.AddMilliseconds(-bufferTimeInMilliseconds);
            return DateTimeOffset.UtcNow.CompareTo(tokenExpiryTimeWithBuffer) >= 0;
        }

    }
}
