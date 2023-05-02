// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    /// <summary>
    /// The exception that is thrown when an error occurs during DeviceClient or ModuleClient operation.
    /// </summary>
    [Serializable]
    public class IotHubException : Exception
    {
        [NonSerialized]
        private const string IsTransientValueSerializationStoreName = "IotHubException-IsTransient";

        [NonSerialized]
        private const string TrackingIdValueSerializationStoreName = "IotHubException-TrackingId";

        /// <summary>
        /// Creates an instance of this class with an empty error message.
        /// </summary>
        public IotHubException() : base()
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public IotHubException(string message)
            : this(message, false)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message and tracking Id, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubException(string message, string trackingId)
            : this(message, false, trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message, tracking Id and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        public IotHubException(string message, bool isTransient, string trackingId)
            : this(message, null, isTransient, trackingId)
        {
        }

        /// <summary>
        /// Creates an instance of this class with the supplied error message and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        public IotHubException(string message, bool isTransient)
            : this(message, null, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of this class with an empty error message and a reference to the inner exception that caused this exception.
        /// </summary>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message and
        /// a reference to the inner exception that caused this exception, and marks it as non-transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubException(string message, Exception innerException)
            : this(message, innerException, false, string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message, a reference
        /// to the inner exception that caused this exception and a flag indicating if the error was transient.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        protected IotHubException(string message, Exception innerException, bool isTransient)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of this class with a specified error message, a reference
        /// to the inner exception that caused this exception, a flag indicating if the error was transient
        /// and the service returned tracking Id associated with this particular error.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        protected IotHubException(string message, Exception innerException, bool isTransient, string trackingId)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            TrackingId = trackingId;
        }

        /// <summary>
        /// Creates an instance of this class with the <see cref="SerializationInfo"/>
        /// and <see cref="StreamingContext"/> associated with the exception.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IotHubException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(TrackingIdValueSerializationStoreName);
            }
        }

        internal IotHubException(bool isTransient)
            : base()
        {
            IsTransient = isTransient;
        }

        /// <summary>
        /// Indicates if the error is transient and should be retried.
        /// </summary>
        public bool IsTransient { get; protected set; }

        /// <summary>
        /// The service returned tracking Id associated with this particular error.
        /// </summary>
        public string TrackingId { get; set; }

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
    }
}
