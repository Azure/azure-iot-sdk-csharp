// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Represents errors reported by the Provisioning Transport Handlers.
    /// </summary>
    public class ProvisioningTransportException : Exception
    {
        private const string IsTransientValueSerializationStoreName = "ProvisioningTransportException-IsTransient";

        /// <summary>
        /// Creates a new instance of the ProvisioningTransportException class.
        /// </summary>
        public ProvisioningTransportException()
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningTransportException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        public ProvisioningTransportException(string message)
            : this(message, null, false)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningTransportException(string message, Exception innerException)
            : this(message, innerException, false, string.Empty)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningTransportException(string message, Exception innerException, bool isTransient)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="trackingId">The service tracking Id.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningTransportException(string message, Exception innerException, bool isTransient, string trackingId)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            TrackingId = trackingId;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="errorDetails">The service error details.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningTransportException(string message, Exception innerException, bool isTransient, ProvisioningErrorDetails errorDetails)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            ErrorDetails = errorDetails;
        }

        /// <summary>
        /// Creates a new instance of the <see cref="ProvisioningTransportException"/> class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ProvisioningTransportException(SerializationInfo info, StreamingContext context)
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
