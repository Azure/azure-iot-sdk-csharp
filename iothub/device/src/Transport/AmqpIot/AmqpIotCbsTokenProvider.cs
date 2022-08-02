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
        private readonly IClientIdentity _clientIdentity;
        private bool _isDisposed;

        public AmqpIotCbsTokenProvider(IClientIdentity clientIdentity)
        {
            _clientIdentity = clientIdentity;
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
                        $"{nameof(IotHubConnectionInfo)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");

                string tokenValue;
                DateTime expiresOn;

                if (!string.IsNullOrWhiteSpace(_clientIdentity.SharedAccessSignature))
                {
                    tokenValue = _clientIdentity.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && _clientIdentity.TokenRefresher == null)
                        Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(AmqpIotCbsTokenProvider.GetTokenAsync));

                    Debug.Assert(_clientIdentity.TokenRefresher != null);
                    tokenValue = await _clientIdentity.TokenRefresher.GetTokenAsync(_clientIdentity.Audience).ConfigureAwait(false);
                    expiresOn = _clientIdentity.TokenRefresher.RefreshesOn;
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
                        $"{nameof(IotHubConnectionInfo)}.{nameof(AmqpIotCbsTokenProvider.GetTokenAsync)}");
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
                    Logging.Enter(this, $"Disposal with client={_clientIdentity?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}" , $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }

                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_clientIdentity?.TokenRefresher != null
                            && _clientIdentity.TokenRefresher.DisposalWithClient)
                        {
                            _clientIdentity.TokenRefresher.Dispose();
                        }
                    }

                    _isDisposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposal with client={_clientIdentity?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}", $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
