// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// HttpClient delegating handler for periodically revoking the connection lease after a specified amount of time. This allows cached HttpClients to periodically do DNS lookups
    /// which keeps the client communicating with the correct service endpoint in failover scenarios.
    /// </summary>
    internal class ConnectionLeaseTimeoutHandler : DelegatingHandler
    {
        private readonly Stopwatch _stopwatch;
        private readonly int _leaseTimeoutMilliseconds;
        private SemaphoreSlim _timeoutCheckSemaphore;

        internal ConnectionLeaseTimeoutHandler(int leaseTimeoutMilliseconds) : base()
        {
            _stopwatch = Stopwatch.StartNew();
            _timeoutCheckSemaphore = new SemaphoreSlim(1, 1);
            _leaseTimeoutMilliseconds = leaseTimeoutMilliseconds;
        }

        protected async override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // By default, singleton/static HttpClient instances will never do a DNS lookup after the first request, which leads to some
            // bugs such as https://github.com/Azure/azure-iot-sdk-csharp/issues/1865

            // Special case for lease timeout is when the value is set to a negative value. In those instances, the lease timeout is infinite,
            // so there is no reason to ever set the ConnectionClose header. As such, there is no need to wait for the semaphore to be available.
            if (_leaseTimeoutMilliseconds >= 0 && _stopwatch.Elapsed.TotalMilliseconds >= _leaseTimeoutMilliseconds)
            {
                // This handler is designed to force the connection to close periodically in order to ensure that httpclients are eventually made aware
                // of any DNS changes from an IoT Hub failing over.
                await _timeoutCheckSemaphore.WaitAsync(cancellationToken).ConfigureAwait(false);

                try
                {
                    // re-check the stopwatch. This thread may have waited for the semaphore thinking it would need to close the connection and reset
                    // the timer, but another thread may have already done that since this thread started waiting.
                    if (_stopwatch.Elapsed.TotalMilliseconds >= _leaseTimeoutMilliseconds)
                    {
                        request.Headers.ConnectionClose = true;
                        _stopwatch.Restart();
                    }
                }
                finally
                {
                    _timeoutCheckSemaphore.Release();
                }
            }

            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        protected override void Dispose(bool disposing)
        {
            _stopwatch.Reset();
            _timeoutCheckSemaphore.Dispose();
            base.Dispose(disposing);
        }
    }
}
