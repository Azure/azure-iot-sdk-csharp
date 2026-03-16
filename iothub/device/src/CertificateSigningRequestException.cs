// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#nullable enable
using System;
using Microsoft.Azure.Devices.Client.Exceptions;
namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Exception thrown when a certificate signing request operation fails.
    /// </summary>
    [Serializable]
    public class CertificateSigningRequestException : IotHubException
    {
        /// <summary>
        /// Creates an instance of <see cref="CertificateSigningRequestException"/> with the specified parameters.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="errorCode">The specific error code from IoT Hub.</param>
        /// <param name="trackingId">Tracking ID for support purposes.</param>
        /// <param name="correlationId">Correlation ID matching the operation.</param>
        /// <param name="certificateSigningRequestError">Certificate signing request specific error name (for 400040 errors).</param>
        /// <param name="retryAfterSeconds">Suggested retry delay in seconds, if applicable.</param>
        /// <param name="activeRequestId">For 409005 errors: the active request ID.</param>
        /// <param name="operationExpires">For 409005 errors: when the active operation expires.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        public CertificateSigningRequestException(
            string message,
            int errorCode,
            string trackingId,
            string? correlationId = null,
            string? certificateSigningRequestError = null,
            int? retryAfterSeconds = null,
            string? activeRequestId = null,
            DateTimeOffset? operationExpires = null,
            bool isTransient = false)
            : base(message, isTransient, trackingId ?? string.Empty)
        {
            ErrorCode = errorCode;
            CorrelationId = correlationId;
            CertificateSigningRequestError = certificateSigningRequestError;
            RetryAfterSeconds = retryAfterSeconds;
            ActiveRequestId = activeRequestId;
            OperationExpires = operationExpires;
        }
        /// <summary>
        /// The specific error code from IoT Hub.
        /// </summary>
        public int ErrorCode { get; }
        /// <summary>
        /// Correlation ID matching the operation.
        /// </summary>
        public string? CorrelationId { get; }
        /// <summary>
        /// Certificate signing request specific error name (for 400040 errors).
        /// </summary>
        public string? CertificateSigningRequestError { get; }
        /// <summary>
        /// Suggested retry delay in seconds, if applicable.
        /// </summary>
        public int? RetryAfterSeconds { get; }
        /// <summary>
        /// For 409005 errors: the active request ID.
        /// Client can check if this matches the last sent request ID.
        /// </summary>
        public string? ActiveRequestId { get; }
        /// <summary>
        /// For 409005 errors: when the active operation expires.
        /// </summary>
        public DateTimeOffset? OperationExpires { get; }
    }
}
