// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Common.Exceptions
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
        /// Creates an instance of <see cref="IotHubServiceException"/> with the supplied error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IotHubServiceException(string message)
            : this(message, false)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with the supplied error message and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubServiceException(string message, string trackingId)
            : this(message, false, trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with the supplied error message, tracking Id and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubServiceException(string message, bool isTransient, string trackingId)
            : this(message, null, isTransient, trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with the supplied error message and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        public IotHubServiceException(string message, bool isTransient)
            : this(message, null, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with an empty error message and a reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubServiceException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubServiceException(string message, Exception innerException)
            : this(message, innerException, false, string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with a specified <see cref="IotHubStatusCode"/>, error message and an
        /// optional reference to the inner exception that caused this exception. This exception is marked as non-transient.
        /// </summary>
        /// <param name="code">The <see cref="IotHubStatusCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubServiceException(IotHubStatusCode code, string message, Exception innerException = null)
            : this(message, innerException, false, string.Empty)
        {
            StatusCode = code;
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with a specified error message, a reference
        /// to the inner exception that caused this exception and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        protected IotHubServiceException(string message, Exception innerException, bool isTransient)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with a specified <see cref="IotHubStatusCode"/>, error message, a flag
        /// indicating if the error was transient, and an optional reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="code">The <see cref="IotHubStatusCode"/> associated with the error.</param>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected IotHubServiceException(IotHubStatusCode code, string message, bool isTransient, Exception innerException = null)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
            StatusCode = code;
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with a specified error message, a reference
        /// to the inner exception that caused this exception, a flag indicating if the error was transient
        /// and the service returned tracking Id associated with this particular error.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        protected IotHubServiceException(string message, Exception innerException, bool isTransient, string trackingId)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            TrackingId = trackingId;
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IotHubServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(TrackingIdSerializationStoreName);
            }
        }

        /// <summary>
        /// Creates an instance of <see cref="IotHubServiceException"/> with an empty error message and marks it as non-transient.
        /// </summary>
        internal IotHubServiceException()
            : base()
        {
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
        /// The error code associated with the exception.
        /// </summary>
        public IotHubStatusCode StatusCode { get; private set; }

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
    }
}
