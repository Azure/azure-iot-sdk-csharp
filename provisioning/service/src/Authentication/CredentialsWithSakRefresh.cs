// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Authentication class implementing SAS token refresh with Shared Access Key
    /// </summary>
    public class CredentialsWithSakRefresh: CredentialsWithTokenRefresh
    {
        private const int DefaultTimeToLiveSeconds = 1 * 60 * 5;
        private const int DefaultBufferPercentage = 15;

        private ServiceConnectionString _connectionString;

        /// <summary>
        /// Initializes a new instance of the <see cref="CredentialsWithSakRefresh"/> class.
        /// </summary>
        /// <param name="connectionString"></param>
        public CredentialsWithSakRefresh(ServiceConnectionString connectionString)
            : base(DefaultTimeToLiveSeconds, DefaultBufferPercentage)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="suggestedTimeToLive"></param>
        /// <returns></returns>
        protected override Task<string> SafeCreateNewToken(int suggestedTimeToLive)
        {
            var builder = new SharedAccessSignatureBuilder()
            {
                KeyName = _connectionString.SharedAccessKeyName,
                Key = _connectionString.SharedAccessKey,
                TimeToLive = TimeSpan.FromSeconds(suggestedTimeToLive),
                Target = _connectionString.HostName
            };

            return Task.FromResult(builder.ToSignature());
        }
    }
}
