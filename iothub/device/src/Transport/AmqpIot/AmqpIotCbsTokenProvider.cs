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
        private readonly IIotHubConnectionInfo _connInfo;
        private bool _isDisposed;

        public AmqpIotCbsTokenProvider(IIotHubConnectionInfo connectionInfo)
        {
            _connInfo = connectionInfo;
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

                if (!string.IsNullOrWhiteSpace(_connInfo.SharedAccessSignature))
                {
                    tokenValue = _connInfo.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && _connInfo.TokenRefresher == null)
                        Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(AmqpIotCbsTokenProvider.GetTokenAsync));

                    Debug.Assert(_connInfo.TokenRefresher != null);
                    tokenValue = await _connInfo.TokenRefresher.GetTokenAsync(_connInfo.Audience).ConfigureAwait(false);
                    expiresOn = _connInfo.TokenRefresher.RefreshesOn;
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
                    Logging.Enter(this, $"Disposal with client={_connInfo?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}" , $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }

                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_connInfo?.TokenRefresher != null
                            && _connInfo.TokenRefresher.DisposalWithClient)
                        {
                            _connInfo.TokenRefresher.Dispose();
                        }
                    }

                    _isDisposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposal with client={_connInfo?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}", $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
