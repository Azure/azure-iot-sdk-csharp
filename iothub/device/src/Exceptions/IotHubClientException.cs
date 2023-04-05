// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
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

        private static readonly HashSet<IotHubClientErrorCode> s_transientErrorCodes = new()
        {
            IotHubClientErrorCode.QuotaExceeded,
            IotHubClientErrorCode.ServerError,
            IotHubClientErrorCode.ServerBusy,
            IotHubClientErrorCode.Throttled,
            IotHubClientErrorCode.Timeout,
            IotHubClientErrorCode.NetworkErrors,
        };

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        public IotHubClientException()
            : base()
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="errorCode">The specific error code.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public IotHubClientException(string message, IotHubClientErrorCode errorCode = IotHubClientErrorCode.Unknown, Exception innerException = null)
            : base(message, innerException)
        {
            ErrorCode = errorCode;
            IsTransient = DetermineIfTransient(errorCode);
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        protected IotHubClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(TrackingIdValueSerializationStoreName);
            }
        }

        /// <summary>
        /// The service returned tracking Id associated with this particular error.
        /// </summary>
        public string TrackingId { get; set; } = string.Empty;

        /// <summary>
        /// Indicates if the error is transient and should be retried.
        /// </summary>
        public bool IsTransient { get; set; }

        /// <summary>
        /// The specific error code.
        /// </summary>
        public IotHubClientErrorCode ErrorCode { get; }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// Use this to set <see cref="IsTransient"/> and <see cref="TrackingId"/> to the serialized object data.
        /// </summary>
        /// <param name="info">The <see cref="SerializationInfo"/> that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The <see cref="StreamingContext"/> that contains contextual information about the source or destination.</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(IsTransientValueSerializationStoreName, IsTransient);
            info.AddValue(TrackingIdValueSerializationStoreName, TrackingId);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Message: {Message}\n" +
                   $"ErrorCode: {ErrorCode}, TrackingId: {TrackingId}, IsTransient: {IsTransient}\n" +
                   $"StackTrace: {StackTrace}";
        }

        private static bool DetermineIfTransient(IotHubClientErrorCode errorCode)
        {
            return s_transientErrorCodes.Contains(errorCode);
        }
    }
}
