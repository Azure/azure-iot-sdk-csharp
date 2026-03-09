// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Represents a two-phase certificate signing operation.
    /// Phase 1 (Accepted): IoT Hub acknowledges the CSR with a 202 response.
    /// Phase 2 (Completed): IoT Hub delivers the issued certificate with a 200 response.
    /// </summary>
    public class CertificateSigningOperation
    {
        private readonly TaskCompletionSource<CertificateAcceptedResponse> _accepted
            = new TaskCompletionSource<CertificateAcceptedResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource<CertificateSigningResponse> _completed
            = new TaskCompletionSource<CertificateSigningResponse>(TaskCreationOptions.RunContinuationsAsynchronously);

        /// <summary>
        /// A task that completes when IoT Hub accepts the certificate signing request (202 Accepted).
        /// The result contains the correlation ID and operation expiration time.
        /// </summary>
        public Task<CertificateAcceptedResponse> Accepted => _accepted.Task;

        /// <summary>
        /// A task that completes when IoT Hub delivers the issued certificate (200 OK).
        /// The result contains the certificate chain and correlation ID.
        /// </summary>
        public Task<CertificateSigningResponse> Completed => _completed.Task;

        internal void SetAccepted(CertificateAcceptedResponse response) => _accepted.TrySetResult(response);

        internal void SetCompleted(CertificateSigningResponse response) => _completed.TrySetResult(response);

        internal void SetFailed(Exception ex)
        {
            _accepted.TrySetException(ex);
            _completed.TrySetException(ex);
        }

        internal void SetCanceled(CancellationToken cancellationToken)
        {
            _accepted.TrySetCanceled(cancellationToken);
            _completed.TrySetCanceled(cancellationToken);
        }
    }
}
