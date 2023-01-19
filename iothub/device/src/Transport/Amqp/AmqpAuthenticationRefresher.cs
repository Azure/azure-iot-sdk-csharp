// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Transport.AmqpIot;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    internal class AmqpAuthenticationRefresher : IAmqpAuthenticationRefresher
    {
        private static readonly string[] s_accessRightsStringArray = new[] { "DeviceConnect" };
        private readonly Uri _amqpEndpoint;
        private readonly AmqpIotCbsLink _amqpIotCbsLink;
        private readonly IConnectionCredentials _connectionCredentials;
        private readonly AmqpIotCbsTokenProvider _amqpIotCbsTokenProvider;
        private readonly string _audience;

        DateTime IAmqpAuthenticationRefresher.SasTokenRefreshesOn { get; set; }

        internal AmqpAuthenticationRefresher(IConnectionCredentials connectionCredentials, AmqpIotCbsLink amqpCbsLink)
        {
            _amqpIotCbsLink = amqpCbsLink;
            _connectionCredentials = connectionCredentials;
            _audience = CreateAmqpCbsAudience(_connectionCredentials);
            _amqpIotCbsTokenProvider = new AmqpIotCbsTokenProvider(_connectionCredentials);
            _amqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, _connectionCredentials.HostName, CommonConstants.DefaultAmqpSecurePort).Uri;

            if (Logging.IsEnabled)
            {
                Logging.Associate(this, _connectionCredentials, nameof(_connectionCredentials));
                Logging.Associate(this, amqpCbsLink, nameof(_amqpIotCbsLink));
            }
        }

        async Task<DateTime> IAmqpAuthenticationRefresher.RefreshSasTokenAsync(CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, nameof(IAmqpAuthenticationRefresher.RefreshSasTokenAsync));

            try
            {
                DateTime refreshesOn = await _amqpIotCbsLink
                    .SendTokenAsync(
                        _amqpIotCbsTokenProvider,
                        _amqpEndpoint,
                        _audience,
                        _audience,
                        s_accessRightsStringArray,
                        cancellationToken)
                    .ConfigureAwait(false);

                if (refreshesOn < DateTime.MaxValue
                    && this is IAmqpAuthenticationRefresher refresher)
                {
                    refresher.SasTokenRefreshesOn = refreshesOn;
                }

                return refreshesOn;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, nameof(IAmqpAuthenticationRefresher.RefreshSasTokenAsync));
            }
        }

        private static string CreateAmqpCbsAudience(IConnectionCredentials connectionCredentials)
        {
            // If the shared access key name is null then this is an individual SAS authenticated client.
            // SAS tokens granted to an individual SAS authenticated client will be scoped to an individual device; for example, myHub.azure-devices.net/devices/device1.
            if (connectionCredentials.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                string clientAudience = $"{connectionCredentials.HostName}/devices/{WebUtility.UrlEncode(connectionCredentials.DeviceId)}";
                if (!connectionCredentials.ModuleId.IsNullOrWhiteSpace())
                {
                    clientAudience += $"/modules/{WebUtility.UrlEncode(connectionCredentials.ModuleId)}";
                }

                return clientAudience;
            }

            // If the shared access key name is not null then this is a group SAS authenticated client.
            // SAS tokens granted to a group SAS authenticated client will scoped to the IoT hub-level; for example, myHub.azure-devices.net
            return connectionCredentials.HostName;
        }
    }
}
