// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Authentication method that generates shared access signature (SAS) token with refresh, based on a provided shared access key (SAK).
    /// </summary>
    public class ClientAuthenticationWithSharedAccessKeyRefresh : ClientAuthenticationWithTokenRefresh
    {
        private static IotHubConnectionString s_iotHubConnectionString;

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="sharedAccessKey">Shared access key value.</param>
        /// <param name="sharedAccessKeyName">Shared access key name.</param>
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
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="sharedAccessKey"/> or <paramref name="sharedAccessKeyName"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sharedAccessKey"/> or <paramref name="sharedAccessKeyName"/> is empty or whitespace.
        /// </exception>
        public ClientAuthenticationWithSharedAccessKeyRefresh(
            string sharedAccessKey,
            string sharedAccessKeyName,
            string deviceId,
            string moduleId = default,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default)
            : base(
                deviceId,
                moduleId,
                sasTokenTimeToLive,
                sasTokenRenewalBuffer)
        {
            Argument.AssertNotNullOrWhiteSpace(sharedAccessKey, nameof(sharedAccessKey));
            SharedAccessKey = sharedAccessKey;

            Argument.AssertNotNullOrWhiteSpace(sharedAccessKeyName, nameof(sharedAccessKeyName));
            SharedAccessKeyName = sharedAccessKeyName;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="sharedAccessKey">Shared access key value.</param>
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
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="sharedAccessKey"/> is null.</exception>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="sharedAccessKey"/> is empty or whitespace.
        /// </exception>
        public ClientAuthenticationWithSharedAccessKeyRefresh(
            string sharedAccessKey,
            string deviceId,
            string moduleId = default,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default)
            : base(
                deviceId,
                moduleId,
                sasTokenTimeToLive,
                sasTokenRenewalBuffer)
        {
            Argument.AssertNotNullOrWhiteSpace(sharedAccessKey, nameof(sharedAccessKey));
            SharedAccessKey = sharedAccessKey;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="connectionString">
        /// The connection string containing the device Id, optional module Id, shared access key name
        /// and shared access key to be used for authenticating with IoT hub service.
        /// </param>
        /// <param name="sasTokenTimeToLive">
        /// The suggested time to live value for the generated SAS tokens.
        /// The default value is 1 hour.
        /// </param>
        /// <param name="sasTokenRenewalBuffer">
        /// The time buffer before expiry when the token should be renewed, expressed as a percentage of the time to live.
        /// The default behavior is that the token will be renewed when it has 15% or less of its lifespan left.
        /// </param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="connectionString"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="connectionString"/> is empty or whitespace.</exception>
        public ClientAuthenticationWithSharedAccessKeyRefresh(
            string connectionString,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default)
            : base(
                // Calling ParseConnectionString will pass the parsed connection string to s_iotHubConnectionString
                ParseConnectionString(connectionString).DeviceId,
                s_iotHubConnectionString.ModuleId,
                sasTokenTimeToLive,
                sasTokenRenewalBuffer)
        {
            SharedAccessKey = s_iotHubConnectionString.SharedAccessKey;
            SharedAccessKeyName = s_iotHubConnectionString.SharedAccessKeyName;
        }

        /// <summary>
        /// Gets the shared access key.
        /// </summary>
        public string SharedAccessKey { get; private set; }

        /// <summary>
        /// Gets the shared access key name.
        /// </summary>
        public string SharedAccessKeyName { get; private set; }

        /// <summary>
        /// Populates an <see cref="IotHubConnectionCredentials"/> instance based on a snapshot of the properties of
        /// the current instance.
        /// </summary>
        /// <param name="iotHubConnectionCredentials">Instance to populate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="iotHubConnectionCredentials"/> is null.</exception>
        public override void Populate(ref IotHubConnectionCredentials iotHubConnectionCredentials)
        {
            Argument.AssertNotNull(iotHubConnectionCredentials, nameof(iotHubConnectionCredentials));

            iotHubConnectionCredentials.SharedAccessSignature = Token;
            iotHubConnectionCredentials.SharedAccessKey = SharedAccessKey;
            iotHubConnectionCredentials.SharedAccessKeyName = SharedAccessKeyName;
            iotHubConnectionCredentials.SasTokenTimeToLive = SuggestedTimeToLive;
            iotHubConnectionCredentials.SasTokenRenewalBuffer = TimeBufferPercentage;
            iotHubConnectionCredentials.DeviceId = DeviceId;
            iotHubConnectionCredentials.ModuleId = ModuleId;
        }

        ///<inheritdoc/>
        protected override Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewTokenAsync));

            var builder = new SharedAccessSignatureBuilder
            {
                Key = SharedAccessKey,
                TimeToLive = suggestedTimeToLive,
            };

            if (SharedAccessKeyName == null)
            {
                builder.Target = ModuleId == default
                    ? "{0}/devices/{1}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId))
                    : "{0}/devices/{1}/modules/{2}".FormatInvariant(
                        iotHub,
                        WebUtility.UrlEncode(DeviceId),
                        WebUtility.UrlEncode(ModuleId));
            }
            else
            {
                builder.KeyName = SharedAccessKeyName;
                builder.Target = iotHub;
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, iotHub, suggestedTimeToLive, nameof(SafeCreateNewTokenAsync));

            return Task.FromResult(builder.ToSignature());
        }

        private static IotHubConnectionString ParseConnectionString(string connectionString)
        {
            return s_iotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
        }
    }
}
