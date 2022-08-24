// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotCbsTokenProvider : ICbsTokenProvider
    {
        private readonly IConnectionCredentials _connectionCredentials;

        public AmqpIotCbsTokenProvider(IConnectionCredentials connectionCredentials)
        {
            _connectionCredentials = connectionCredentials;
        }

        public async Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
            try
            {
                if (Logging.IsEnabled)
                    Logging.Enter(
                        this,
                        namespaceAddress,
                        appliesTo,
                        $"{nameof(IotHubConnectionCredentials)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");

                string tokenValue;
                DateTime expiresOn;

                if (!string.IsNullOrWhiteSpace(_connectionCredentials.SharedAccessSignature))
                {
                    tokenValue = _connectionCredentials.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && _connectionCredentials.SasTokenRefresher == null)
                        Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(AmqpIotCbsTokenProvider.GetTokenAsync));

                    Debug.Assert(_connectionCredentials.SasTokenRefresher != null);
                    tokenValue = await _connectionCredentials.SasTokenRefresher
                        .GetTokenAsync(_connectionCredentials.HostName)
                        .ConfigureAwait(false);
                    expiresOn = _connectionCredentials.SasTokenRefresher.RefreshesOn;
                }

                return new CbsToken(tokenValue, AmqpIotConstants.IotHubSasTokenType, expiresOn);
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(
                        this,
                        namespaceAddress,
                        appliesTo,
                        $"{nameof(IotHubConnectionCredentials)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");
            }
        }
    }
}
