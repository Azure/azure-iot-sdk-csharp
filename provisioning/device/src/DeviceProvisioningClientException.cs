// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The exception that is thrown when an error occurs during device provisioning client operation.
    /// </summary>
    public class DeviceProvisioningClientException : Exception
    {
        private const string IsTransientValueSerializationStoreName = "DeviceProvisioningClientException-IsTransient";

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        protected internal DeviceProvisioningClientException(string message, bool isTransient)
            : this(message, null, isTransient)
        {
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="innerException">The inner exception.</param>
        protected internal DeviceProvisioningClientException(string message, Exception innerException, bool isTransient)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="errorDetails">The service error details.</param>
        /// <param name="innerException">The inner exception.</param>
        protected internal DeviceProvisioningClientException(string message, Exception innerException, bool isTransient, ProvisioningErrorDetails errorDetails)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            ErrorDetails = errorDetails;
            StatusCode = (HttpStatusCode)(errorDetails.ErrorCode/1000);
            TrackingId = errorDetails.TrackingId;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected internal DeviceProvisioningClientException(SerializationInfo info, StreamingContext context)
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
        public bool IsTransient { get; private set; }

        /// <summary>
        /// Service reported tracking Id. Use this when reporting a service issue.
        /// </summary>
        public string TrackingId { get; set; }

        /// <summary>
        /// Service reported error details. Use this when reporting a service issue.
        /// </summary>
        public ProvisioningErrorDetails ErrorDetails { get; private set; }

        /// <summary>
        /// The 3-digit HTTP status code returned by Device Provisioning Service.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

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
