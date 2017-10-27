// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    public abstract class ProvisioningTransportHandler : IDisposable
    {
        private ProvisioningTransportHandler _innerHandler;

        public ProvisioningTransportHandler() {}

        public ProvisioningTransportHandler(ProvisioningTransportHandler innerHandler)
        {
            _innerHandler = innerHandler;
        }

        public ProvisioningTransportHandler InnerHandler
        {
            get
            {
                return _innerHandler;
            }
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                if (Logging.IsEnabled) Logging.Associate(this, value);
                _innerHandler = value;
            }
        }

        public virtual Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            return _innerHandler.RegisterAsync(message, cancellationToken);
        }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProvisioningTransportHandler()
        {
            Dispose(false);
        }
    }
}
