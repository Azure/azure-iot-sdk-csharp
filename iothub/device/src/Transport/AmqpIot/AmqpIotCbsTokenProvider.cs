// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal class AmqpIotCbsTokenProvider : ICbsTokenProvider, IDisposable
    {
        private readonly IClientConfiguration _clientConfiguration;
        private bool _isDisposed;

        public AmqpIotCbsTokenProvider(IClientConfiguration clientConfiguration)
        {
            _clientConfiguration = clientConfiguration;
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
                        $"{nameof(ClientConfiguration)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");

                string tokenValue;
                DateTime expiresOn;

                if (!string.IsNullOrWhiteSpace(_clientConfiguration.SharedAccessSignature))
                {
                    tokenValue = _clientConfiguration.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && _clientConfiguration.TokenRefresher == null)
                        Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(AmqpIotCbsTokenProvider.GetTokenAsync));

                    Debug.Assert(_clientConfiguration.TokenRefresher != null);
                    tokenValue = await _clientConfiguration.TokenRefresher.GetTokenAsync(_clientConfiguration.IotHubHostName).ConfigureAwait(false);
                    expiresOn = _clientConfiguration.TokenRefresher.RefreshesOn;
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
                        $"{nameof(ClientConfiguration)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");
            }
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(
                        this,
                        $"Disposal with client={_clientConfiguration?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}" ,
                        $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }

                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_clientConfiguration?.TokenRefresher != null
                            && _clientConfiguration.TokenRefresher.DisposalWithClient)
                        {
                            _clientConfiguration.TokenRefresher.Dispose();
                        }
                    }

                    _isDisposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(
                        this,
                        $"Disposal with client={_clientConfiguration?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}",
                        $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
