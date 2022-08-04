// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents the interface for a Provisioning Transport Handler.
    /// </summary>
    public abstract class ProvisioningTransportHandler : IDisposable
    {
        private ProvisioningTransportHandler _innerHandler;
        private int _port;

        /// <summary>
        /// Creates an instance of the ProvisioningTransportHandler class.
        /// </summary>
        public ProvisioningTransportHandler() { }

        /// <summary>
        /// Gets or sets the proxy for Provisioning Client operations.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// A callback for remote certificate validation.
        /// If incorrectly implemented, your device may fail to connect to DPS and/or be open to security vulnerabilities.
        /// </summary>
        public RemoteCertificateValidationCallback RemoteCertificateValidationCallback { get; set; }

        /// <summary>
        /// Gets or sets the inner handler.
        /// </summary>
        public ProvisioningTransportHandler InnerHandler
        {
            get => _innerHandler;
            set
            {
                if (value == null)
                {
                    throw new ArgumentNullException(nameof(value));
                }

                Logging.Associate(this, value);

                _innerHandler = value;
            }
        }

        /// <summary>
        /// Gets or sets the port number.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                if (value < 1 || value > 65535)
                {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }

                Logging.Info(this, $"{nameof(Port)} set to {value}");

                _port = value;
            }
        }

        /// <summary>
        /// The version of TLS to use by default.
        /// </summary>
        /// <remarks>
        /// Defaults to "None", which means let the OS decide the proper TLS version (SChannel in Windows / OpenSSL in Linux).
        /// </remarks>
        public SslProtocols SslProtocols { get; set; } = SslProtocols.None;

        /// <summary>
        /// To enable certificate revocation check.
        /// </summary>
        /// <remarks>
        /// Defaults to false.
        /// </remarks>
        public bool CertificateRevocationCheck { get; set; }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The registration result.</returns>
        public virtual Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
            CancellationToken cancellationToken)
        {
            return _innerHandler.RegisterAsync(message, cancellationToken);
        }

        /// <summary>
        /// Registers a device described by the message.
        /// </summary>
        /// <param name="message">The provisioning message.</param>
        /// <param name="timeout">The maximum amount of time to allow this operation to run for before timing out.</param>
        /// <returns>The registration result.</returns>
        public virtual Task<DeviceRegistrationResult> RegisterAsync(
            ProvisioningTransportRegisterRequest message,
            TimeSpan timeout)
        {
            return _innerHandler.RegisterAsync(message, timeout);
        }

        /// <summary>
        /// Releases the unmanaged resources and disposes of the managed resources used by the invoker.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
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
    }
}
