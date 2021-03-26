﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotCbsTokenProvider : ICbsTokenProvider
    {
        private readonly IotHubConnectionString _connectionString;

        public AmqpIotCbsTokenProvider(IotHubConnectionString connectionString)
        {
            _connectionString = connectionString;
        }

        public async Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, namespaceAddress, appliesTo, $"{nameof(IotHubConnectionString)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");
                }

                string tokenValue;
                DateTime expiresOn;

                if (!string.IsNullOrWhiteSpace(_connectionString.SharedAccessSignature))
                {
                    tokenValue = _connectionString.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && _connectionString.TokenRefresher == null)
                    {
                        Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(AmqpIotCbsTokenProvider.GetTokenAsync));
                    }

                    Debug.Assert(_connectionString.TokenRefresher != null);
                    tokenValue = await _connectionString.TokenRefresher.GetTokenAsync(_connectionString.Audience).ConfigureAwait(false);
                    expiresOn = _connectionString.TokenRefresher.RefreshesOn;
                }

                return new CbsToken(tokenValue, AmqpIotConstants.IotHubSasTokenType, expiresOn);
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, namespaceAddress, appliesTo, $"{nameof(IotHubConnectionString)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");
                }
            }
        }
    }
}
