// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.Serialization;
using System.Text;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The exception thrown when the client receives an error while communicating with IoT hub service.
    /// </summary>
    [Serializable]
    public class IotHubServiceException : Exception
    {
        [NonSerialized]
        private const string IsTransientValueSerializationStoreName = "IotHubServiceException-IsTransient";

        [NonSerialized]
        private const string TrackingIdValueSerializationStoreName = "IotHubServiceException-TrackingId";

        /// <summary>
        /// Creates an instance of this class with the specified error message and optional inner exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">An inner exception, if any.</param>
        public IotHubServiceException(string message, Exception innerException = default)
            : base(message, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class with <see cref="HttpStatusCode"/>, <see cref="IotHubServiceErrorCode"/>, 
        /// error message, a flag indicating if the error was transient, an optional tracking id and an optional reference
        /// to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The 3-digit status code returned back in the hub service response.</param>
        /// <param name="errorCode">The 6-digit error code representing a more specific error in details.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubServiceException(
            string message,
            HttpStatusCode statusCode,
            IotHubServiceErrorCode errorCode,
            string trackingId = null,
            Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            IsTransient = DetermineIfTransient(statusCode, errorCode);
            TrackingId = trackingId;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="info"/> is null.</exception>
        protected IotHubServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(TrackingIdValueSerializationStoreName);
            }
        }

        /// <summary>
        /// Indicates if the error is transient and should be retried.
        /// </summary>
        public bool IsTransient { get; protected internal set; }

        /// <summary>
        /// The service returned tracking Id associated with this particular error.
        /// </summary>
        public string TrackingId { get; protected internal set; }

        /// <summary>
        /// The status code returned back in the IoT hub service response.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The specific error code in the IoT hub service response, if available.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/rest/api/iothub/common-error-codes"/>.
        public IotHubServiceErrorCode ErrorCode { get; }

        /// <inheritdoc/>
        public override string ToString()
        {
            var sb = new StringBuilder($"Message: {Message}\nErrorCode: {ErrorCode}, StatusCode: {StatusCode}, IsTransient: {IsTransient}");
            if (!string.IsNullOrEmpty(TrackingId))
            {
                sb.Append($", TrackingId: {TrackingId}");
            }
            sb.Append($"\n{StackTrace}");
            return sb.ToString();
        }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// Use this to set <see cref="IsTransient"/> and <see cref="TrackingId"/> to the serialized object data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="info"/> is null.</exception>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(IsTransientValueSerializationStoreName, IsTransient);
            info.AddValue(TrackingIdValueSerializationStoreName, TrackingId);
        }

        private static bool DetermineIfTransient(HttpStatusCode statusCode, IotHubServiceErrorCode errorCode)
        {
            return errorCode switch
            {
                IotHubServiceErrorCode.IotHubQuotaExceeded
                    or IotHubServiceErrorCode.DeviceNotOnline
                    or IotHubServiceErrorCode.GenericTooManyRequests
                    or IotHubServiceErrorCode.ThrottlingException
                    or IotHubServiceErrorCode.ThrottleBacklogLimitExceeded
                    or IotHubServiceErrorCode.ThrottlingBacklogTimeout
                    or IotHubServiceErrorCode.ThrottlingMaxActiveJobCountExceeded
                    or IotHubServiceErrorCode.DeviceThrottlingLimitExceeded
                    or IotHubServiceErrorCode.ServerError
                    or IotHubServiceErrorCode.ServiceUnavailable
                    or IotHubServiceErrorCode.RequestTimeout
                    => true,
                IotHubServiceErrorCode.Unknown
                    => statusCode == (HttpStatusCode)429,
                _ => false,
            };
        }
    }
}
