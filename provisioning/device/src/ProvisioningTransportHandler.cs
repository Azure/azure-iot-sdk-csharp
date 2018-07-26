// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    /// <summary>
    /// Represents the interface for a Provisioning Transport Handler.
    /// </summary>
    public abstract class ProvisioningTransportHandler : IDisposable
    {
        private ProvisioningTransportHandler _innerHandler;
        private int _port;

        /// <summary>
        /// Gets or sets the proxy for Provisioning Client operations.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandler class.
        /// </summary>
        public ProvisioningTransportHandler() {}

        /// <summary>
        /// Gets or sets the inner handler.
        /// </summary>
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

        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        public int Port
        {
            get
            {
                return _port;
            }
            set
            {
                if (value < 1 || value > 65535)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                if (Logging.IsEnabled) Logging.Info(this, $"{nameof(Port)} set to {value}");
                _port = value;
            }
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public virtual Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterMessage message,
            CancellationToken cancellationToken)
        {
            return _innerHandler.RegisterAsync(message, cancellationToken);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the ProvisioningTransportHandler and optionally disposes of the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to releases only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerHandler?.Dispose();
            }
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used by the invoker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
