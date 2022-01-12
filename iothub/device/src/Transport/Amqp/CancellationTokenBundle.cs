// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;

namespace Microsoft.Azure.Devices.Client.Transport.Amqp
{
    /// <summary>
    /// Links different sources of cancellation to bundle them together.
    /// </summary>
    internal class CancellationTokenBundle : IDisposable
    {
        private readonly CancellationTokenSource _timeoutTokenSource;
        private readonly CancellationTokenSource _bundleSource;
        private bool _isDisposed;

        /// <summary>
        /// Creates a linked source of cancellation from multiple sources.
        /// </summary>
        /// <param name="timeout">The timeout from which a cancellation token source is created.</param>
        /// <param name="cancellationToken">The cancellation token to be used in a linked cancellation token source.</param>
        public CancellationTokenBundle(TimeSpan timeout, CancellationToken cancellationToken)
        {
            _timeoutTokenSource = new CancellationTokenSource(timeout);
            _bundleSource = CancellationTokenSource.CreateLinkedTokenSource(_timeoutTokenSource.Token, cancellationToken);
        }

        /// <summary>
        /// The linked token from the cancellation bundle.
        /// </summary>
        public CancellationToken Token => _bundleSource.Token;

        public void Dispose()
        {
            if (_isDisposed)
            {
                return;
            }

            _timeoutTokenSource.Dispose();
            _bundleSource.Dispose();
            _isDisposed = true;
        }
    }
}
