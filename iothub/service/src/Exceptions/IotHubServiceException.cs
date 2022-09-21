// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The exception thrown when the client receives an error while communicating with IoT hub service.
    /// </summary>
    [Serializable]
    public class IotHubServiceException : Exception
    {
        private const string IsTransientValueSerializationStoreName = "IotHubServiceException-IsTransient";
        private const string TrackingIdSerializationStoreName = "IoTHubException-TrackingId";

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
        /// Creates an instance of this class with <see cref="HttpStatusCode"/>, <see cref="IotHubErrorCode"/>, 
        /// error message, a flag indicating if the error was transient, an optional tracking id and an optional reference
        /// to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The 3-digit status code returned back in the hub service response.</param>
        /// <param name="errorCode">The 6-digit error code representing a more specific error in details.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubServiceException(string message, HttpStatusCode statusCode, IotHubErrorCode errorCode, string trackingId = null, Exception innerException = null)
            : base(message, innerException)
        {
            StatusCode = statusCode;
            ErrorCode = errorCode;
            IsTransient = DetermineIfTransient(statusCode, errorCode);
            TrackingId = trackingId;
        }

        /// <summary>
        /// Indicates if the error is transient and should be retried.
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// The service returned tracking Id associated with this particular error.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// The status code returned back in the IoT hub service response.
        /// </summary>
        public HttpStatusCode StatusCode { get; set; }

        /// <summary>
        /// The specific error code in the IoT hub service response, if available.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/rest/api/iothub/common-error-codes"/>.
        public IotHubErrorCode ErrorCode { get; set; }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// Use this to set <see cref="IsTransient"/> and <see cref="TrackingId"/> to the serialized object data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(IsTransientValueSerializationStoreName, IsTransient);
            info.AddValue(TrackingIdSerializationStoreName, TrackingId);
        }

        private bool DetermineIfTransient(HttpStatusCode statusCode, IotHubErrorCode errorCode)
        {
            switch (errorCode)
            {
                case IotHubErrorCode.IotHubQuotaExceeded:
                case IotHubErrorCode.DeviceNotOnline:
                case IotHubErrorCode.ThrottlingException:
                case IotHubErrorCode.ServerError:
                case IotHubErrorCode.ServiceUnavailable:
                    return true;

                case IotHubErrorCode.Unknown:
                    return statusCode == HttpStatusCode.RequestTimeout;

                default:
                    return false;
            }
        }
    }
}
