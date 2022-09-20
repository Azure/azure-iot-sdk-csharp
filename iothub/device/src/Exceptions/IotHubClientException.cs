// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an error occurs during DeviceClient or ModuleClient operation.
    /// </summary>
    [Serializable]
    public class IotHubClientException : Exception
    {
        [NonSerialized]
        private const string IsTransientValueSerializationStoreName = "IotHubClientException-IsTransient";

        [NonSerialized]
        private const string TrackingIdValueSerializationStoreName = "IotHubClientException-TrackingId";

        private HashSet<IotHubStatusCode> transientStatusCodes = new HashSet<IotHubStatusCode>
        {
            IotHubStatusCode.QuotaExceeded,
            IotHubStatusCode.ServerError,
            IotHubStatusCode.ServerBusy,
            IotHubStatusCode.Throttled,
            IotHubStatusCode.Timeout,
            IotHubStatusCode.NetworkErrors,
        };

        /// <summary>
        /// Creates an instance of this class with an empty error message.
        /// </summary>
        protected internal IotHubClientException() : base()
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected internal IotHubClientException(string message)
            : this(message, false)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        protected internal IotHubClientException(string message, bool isTransient)
            : this(message, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message and the HTTP status statusCode returned by the IoT Hub service.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The HTTP status statusCode returned by the IoT hub service.</param>
        protected internal IotHubClientException(string message, IotHubStatusCode statusCode)
            : this(message, trackingId: string.Empty, statusCode)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and an optional reference to
        /// the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, Exception innerException = null)
            : this(message, false, string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message, a flag indicating if the
        /// error was transient and an optional reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, bool isTransient, Exception innerException = null)
            : this(message, isTransient, trackingId: string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message, the HTTP status statusCode returned
        /// by the IoT Hub service and an optional reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="statusCode">The HTTP status statusCode returned by the IoT hub service.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, IotHubStatusCode statusCode, Exception innerException = null)
            : this(message, trackingId: string.Empty, statusCode, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message, a flag indicating if the error
        /// was transient, the service returned tracking Id associated with this particular error and an optional
        /// reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, bool isTransient, string trackingId, Exception innerException = null)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            TrackingId = trackingId;
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message, the service returned tracking Id associated with this particular error,
        /// the HTTP status statusCode returned by the IoT Hub service and an optional reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        /// <param name="statusCode">The HTTP status statusCode returned by the IoT hub service.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, string trackingId, IotHubStatusCode statusCode, Exception innerException = null)
            : base(message, innerException)
        {
            IsTransient = DetermineIfTransient(statusCode);
            TrackingId = trackingId;
            StatusCode = statusCode;
        }

        /// <summary>
        /// Creates an instance of this class with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected internal IotHubClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(TrackingIdValueSerializationStoreName);
            }
        }

        internal IotHubClientException(bool isTransient) : base()
        {
            IsTransient = isTransient;
        }

        internal IotHubClientException(bool isTransient, IotHubStatusCode statusCode) : base()
        {
            IsTransient = isTransient;
            StatusCode = statusCode;
        }

        /// <summary>
        /// Indicates if the error is transient and should be retried.
        /// </summary>
        public bool IsTransient { get; private set; }

        /// <summary>
        /// The service returned tracking Id associated with this particular error.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// The HTTP status statusCode returned by the IoT hub service.
        /// </summary>
        public IotHubStatusCode StatusCode { get; internal set; }

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
            info.AddValue(TrackingIdValueSerializationStoreName, TrackingId);
        }

        private bool DetermineIfTransient(IotHubStatusCode statusCode)
        {
            return transientStatusCodes.Contains(statusCode);
        }
    }
}
