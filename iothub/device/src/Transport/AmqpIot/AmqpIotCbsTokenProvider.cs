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
        private readonly IDeviceIdentity _deviceIdentity;
        private bool _isDisposed;

        public AmqpIotCbsTokenProvider(IDeviceIdentity deviceIdentity)
        {
            _deviceIdentity = deviceIdentity;
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

                if (!string.IsNullOrWhiteSpace(_deviceIdentity.SharedAccessSignature))
                {
                    tokenValue = _deviceIdentity.SharedAccessSignature;
                    expiresOn = DateTime.MaxValue;
                }
                else
                {
                    if (Logging.IsEnabled && _deviceIdentity.TokenRefresher == null)
                        Logging.Fail(this, $"Cannot create SAS Token: no provider.", nameof(AmqpIotCbsTokenProvider.GetTokenAsync));

                    Debug.Assert(_deviceIdentity.TokenRefresher != null);
                    tokenValue = await _deviceIdentity.TokenRefresher.GetTokenAsync(_deviceIdentity.Audience).ConfigureAwait(false);
                    expiresOn = _deviceIdentity.TokenRefresher.RefreshesOn;
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
                    Logging.Enter(this, $"Disposal with client={_deviceIdentity?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}" , $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }

                if (!_isDisposed)
                {
                    if (disposing)
                    {
                        if (_deviceIdentity?.TokenRefresher != null
                            && _deviceIdentity.TokenRefresher.DisposalWithClient)
                        {
                            _deviceIdentity.TokenRefresher.Dispose();
                        }
                    }

                    _isDisposed = true;
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"Disposal with client={_deviceIdentity?.TokenRefresher?.DisposalWithClient}; disposed={_isDisposed}", $"{nameof(AmqpIotCbsTokenProvider)}.{nameof(Dispose)}");
                }
            }
        }
    }
}
