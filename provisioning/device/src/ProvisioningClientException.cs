// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The exception that is thrown when an error occurs during device provisioning client operation.
    /// </summary>
    public class ProvisioningClientException : Exception
    {
        private const string IsTransientValueSerializationStoreName = "ProvisioningClientException-IsTransient";

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        protected internal ProvisioningClientException(string message, bool isTransient)
            : this(message, null, isTransient)
        {
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="innerException">The inner exception.</param>
        protected internal ProvisioningClientException(string message, Exception innerException, bool isTransient)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        /// <param name="isTransient">If the error is transient and the application should retry at a later time.</param>
        /// <param name="errorCode">The specific 6-digit error code in the DPS response, if available.</param>
        /// <param name="trackingId">Service reported tracking Id.</param>
        protected internal ProvisioningClientException(string message, Exception innerException, bool isTransient, int errorCode, string trackingId)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            ErrorCode = errorCode;
            TrackingId = trackingId;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected internal ProvisioningClientException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(IsTransientValueSerializationStoreName);
            }
        }

        /// <summary>
        /// If true, the error is transient and the application should retry at a later time.
        /// </summary>
        public bool IsTransient { get; }

        /// <summary>
        /// Service reported tracking Id. Use this when reporting a service issue.
        /// </summary>
        public string TrackingId { get; }

        /// <summary>
        /// The specific 6-digit error code in the DPS response, if available.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// Sets the <see cref="SerializationInfo"/> with information about the exception.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(IsTransientValueSerializationStoreName, IsTransient);
        }
    }
}
