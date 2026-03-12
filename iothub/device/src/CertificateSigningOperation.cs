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
    /// <remarks>
    /// If the operation fails at any point, both <see cref="Accepted"/> and <see cref="Completed"/> will
    /// throw a <see cref="CertificateSigningRequestException"/> when awaited. The exception contains
    /// structured error details such as <see cref="CertificateSigningRequestException.ErrorCode"/>,
    /// <see cref="CertificateSigningRequestException.RetryAfterSeconds"/>, and for 409005 conflict errors,
    /// <see cref="CertificateSigningRequestException.ActiveRequestId"/>.
    /// <code>
    /// try
    /// {
    ///     CertificateAcceptedResponse accepted = await operation.Accepted;
    ///     CertificateSigningResponse completed = await operation.Completed;
    /// }
    /// catch (CertificateSigningRequestException ex) when (ex.ErrorCode == 409005)
    /// {
    ///     // Conflict: another CSR operation is active. Use Replace = "*" to override.
    /// }
    /// catch (CertificateSigningRequestException ex) when (ex.RetryAfterSeconds.HasValue)
    /// {
    ///     await Task.Delay(TimeSpan.FromSeconds(ex.RetryAfterSeconds.Value));
    ///     // Retry the operation.
    /// }
    /// </code>
    /// </remarks>
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
        /// <exception cref="CertificateSigningRequestException">
        /// Thrown when the CSR is rejected by IoT Hub. Inspect <see cref="CertificateSigningRequestException.ErrorCode"/>
        /// for the specific failure reason (e.g., 400040 for CSR decode failure, 409005 for an active conflicting operation,
        /// 429002/429003 for throttling).
        /// </exception>
        public Task<CertificateAcceptedResponse> Accepted => _accepted.Task;

        /// <summary>
        /// A task that completes when IoT Hub delivers the issued certificate (200 OK).
        /// The result contains the certificate chain and correlation ID.
        /// </summary>
        /// <exception cref="CertificateSigningRequestException">
        /// Thrown when the certificate issuance fails after acceptance. This can also be thrown if the initial
        /// request was rejected, since a failure at any phase propagates to both <see cref="Accepted"/> and
        /// <see cref="Completed"/> tasks.
        /// </exception>
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
