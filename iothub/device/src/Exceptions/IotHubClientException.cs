// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Client
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

        private HashSet<IotHubClientErrorCode> transientErrorCodes = new HashSet<IotHubClientErrorCode>
        {
            IotHubClientErrorCode.QuotaExceeded,
            IotHubClientErrorCode.ServerError,
            IotHubClientErrorCode.ServerBusy,
            IotHubClientErrorCode.Throttled,
            IotHubClientErrorCode.Timeout,
            IotHubClientErrorCode.NetworkErrors,
        };

        private Dictionary<IotHubClientErrorCode, HttpStatusCode> httpStatusCodes = new Dictionary<IotHubClientErrorCode, HttpStatusCode>()
        {
            { IotHubClientErrorCode.Ok, HttpStatusCode.OK },
            { IotHubClientErrorCode.DeviceMaximumQueueDepthExceeded, HttpStatusCode.Forbidden },
            { IotHubClientErrorCode.QuotaExceeded, HttpStatusCode.Forbidden },
            { IotHubClientErrorCode.DeviceMessageLockLost, HttpStatusCode.PreconditionFailed },
            { IotHubClientErrorCode.DeviceNotFound, HttpStatusCode.NotFound },
            { IotHubClientErrorCode.NetworkErrors, HttpStatusCode.BadRequest },
            { IotHubClientErrorCode.Suspended, HttpStatusCode.BadRequest },
            { IotHubClientErrorCode.Timeout, HttpStatusCode.RequestTimeout },
            { IotHubClientErrorCode.Throttled, (HttpStatusCode)429 },
            { IotHubClientErrorCode.PreconditionFailed, HttpStatusCode.PreconditionFailed },
            { IotHubClientErrorCode.MessageTooLarge, HttpStatusCode.RequestEntityTooLarge },
            { IotHubClientErrorCode.ServerBusy, HttpStatusCode.ServiceUnavailable },
            { IotHubClientErrorCode.ServerError, HttpStatusCode.InternalServerError },
            { IotHubClientErrorCode.Unauthorized, HttpStatusCode.Unauthorized },
        };

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        protected internal IotHubClientException() : base()
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        protected internal IotHubClientException(string message)
            : this(message, false)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        protected internal IotHubClientException(string message, bool isTransient)
            : this(message, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The specific error code.</param>
        protected internal IotHubClientException(string message, IotHubClientErrorCode errorCode)
            : this(message, trackingId: string.Empty, errorCode)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, Exception innerException = null)
            : this(message, false, string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="isTransient">Indicates if the error is transient and should be retried.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, bool isTransient, Exception innerException = null)
            : this(message, isTransient, trackingId: string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The specific error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, IotHubClientErrorCode errorCode, Exception innerException = null)
            : this(message, trackingId: string.Empty, errorCode, innerException)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
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
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="trackingId">The service returned tracking Id associated with this particular error.</param>
        /// <param name="errorCode">The specific error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        protected internal IotHubClientException(string message, string trackingId, IotHubClientErrorCode errorCode, Exception innerException = null)
            : base(message, innerException)
        {
            IsTransient = DetermineIfTransient(errorCode);
            TrackingId = trackingId;
            ErrorCode = errorCode;

            if (httpStatusCodes.TryGetValue(errorCode, out HttpStatusCode value))
            {
                StatusCode = value;
            }
            else
            {
                StatusCode = 0;
            }
        }

        /// <summary>
        /// Creates an instance of this class.
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

        internal IotHubClientException(IotHubClientErrorCode errorCode) : base()
        {
            IsTransient = DetermineIfTransient(errorCode);
            ErrorCode = errorCode;
            
            if (httpStatusCodes.TryGetValue(errorCode, out HttpStatusCode value))
            {
                StatusCode = value;
            }
            else
            {
                StatusCode = 0;
            }
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
        /// The HTTP status code.
        /// </summary>
        /// <remarks>
        /// This property is not actually obtained from the response, but mapped from the property <see cref="ErrorCode"/> with the best effort.
        /// For more details, check <see cref="httpStatusCodes"/>.
        /// </remarks>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// The specific error code.
        /// </summary>
        public IotHubClientErrorCode ErrorCode { get; internal set; }

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

        private bool DetermineIfTransient(IotHubClientErrorCode errorCode)
        {
            return transientErrorCodes.Contains(errorCode);
        }
    }
}
