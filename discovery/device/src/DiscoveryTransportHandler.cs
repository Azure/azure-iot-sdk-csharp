// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Security;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Discovery.Client.Transport
{
    /// <summary>
    /// Represents the interface for a Discovery Transport Handler.
    /// </summary>
    public abstract class DiscoveryTransportHandler : IDisposable
    {
        private DiscoveryTransportHandler _innerHandler;
        private int _port;

        /// <summary>
        /// Creates an instance of the <see cref="DiscoveryTransportHandler"/> class.
        /// </summary>
        public DiscoveryTransportHandler() { }

        /// <summary>
        /// Gets or sets the proxy for Discovery Client operations.
        /// </summary>
        public IWebProxy Proxy { get; set; }

        /// <summary>
        /// Gets or sets the inner handler.
        /// </summary>
        public DiscoveryTransportHandler InnerHandler
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
        /// Issue TPM Challenge. Will return an encrypted nonce, that can be used to
        /// sign a SAS Token for the GetOnboardingInfo request.
        /// </summary>
        /// <returns></returns>
        public virtual Task<string> IssueChallengeAsync(DiscoveryTransportIssueChallengeRequest request, CancellationToken cancellationToken)
        {
            return _innerHandler.IssueChallengeAsync(request, cancellationToken);
        }

        /// <summary>
        /// Get onboarding info
        /// </summary>
        /// <returns></returns>
        public virtual Task<OnboardingInfo> GetOnboardingInfoAsync(DiscoveryTransportGetOnboardingInfoRequest request, CancellationToken cancellationToken)
        {
            return _innerHandler.GetOnboardingInfoAsync(request, cancellationToken);
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
        /// Releases the unmanaged resources used by the <see cref="DiscoveryTransportHandler"/> and optionally disposes of the managed resources.
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
