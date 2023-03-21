// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service exceptions on the Service Client.
    /// </summary>
    [Serializable]
    public class ProvisioningServiceException : Exception
    {
        [NonSerialized]
        private const string IsTransientValueSerializationStoreName = "ProvisioningServiceException-IsTransient";

        [NonSerialized]
        private const string TrackingIdValueSerializationStoreName = "ProvisioningServiceException-TrackingId";

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message.</param>
        public ProvisioningServiceException(string message)
            : this(message, innerException: null, isTransient: false)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="isTransient">True if the error is transient and the operation should be retried.</param>
        public ProvisioningServiceException(string message, bool isTransient)
            : this(message, innerException: null, isTransient: isTransient)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningServiceException(Exception innerException)
            : this(string.Empty, innerException, isTransient: false)
        {
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        /// <param name="isTransient">True if the error is transient and the operation should be retried.</param>
        public ProvisioningServiceException(string message, Exception innerException, bool isTransient)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="statusCode">The 3-digit HTTP status code returned by Device Provisioning Service.</param>
        /// <param name="innerException">The inner exception</param>
        public ProvisioningServiceException(string message, HttpStatusCode statusCode, Exception innerException = null)
            : base(message, innerException)
        {
            IsTransient = DetermineIfTransient(statusCode);
            StatusCode = statusCode;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="statusCode">The 3-digit HTTP status code returned by Device Provisioning Service.</param>
        /// <param name="fields">The HTTP headers.</param>
        public ProvisioningServiceException(string message, HttpStatusCode statusCode, IDictionary<string, string> fields)
            : base(message)
        {
            IsTransient = DetermineIfTransient(statusCode);
            StatusCode = statusCode;
            Fields = fields;
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="statusCode">The 3-digit HTTP status code returned by Device Provisioning Service.</param>
        /// <param name="errorCode">The specific 6-digit error code in the DPS response, if available.</param>
        /// <param name="trackingId">Service reported tracking Id. Use this when reporting a service issue.</param>
        /// <param name="fields">The HTTP headers.</param>
        public ProvisioningServiceException(string message, HttpStatusCode statusCode, int errorCode, string trackingId, IDictionary<string, string> fields)
            : base(message)
        {
            IsTransient = DetermineIfTransient(statusCode);
            StatusCode = statusCode;
            ErrorCode = errorCode;
            TrackingId = trackingId;
            Fields = fields;
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="info">The SerializationInfo that holds the serialized object data about the exception being thrown.</param>
        /// <param name="context">The StreamingContext that contains contextual information about the source or destination.</param>
        protected ProvisioningServiceException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                IsTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                TrackingId = info.GetString(TrackingIdValueSerializationStoreName);
            }
        }

        /// <summary>
        /// True if the error is transient.
        /// </summary>
        public bool IsTransient { get; }

        /// <summary>
        /// Service reported tracking Id. Use this when reporting a service issue.
        /// </summary>
        public string TrackingId { get; }

        /// <summary>
        /// The 3-digit HTTP status code returned by Device Provisioning Service.
        /// </summary>
        public HttpStatusCode StatusCode { get; }

        /// <summary>
        /// The specific 6-digit error code in the DPS response, if available.
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// The HTTP headers.
        /// </summary>
        /// <remarks>
        /// This is used by DPS E2E tests.
        /// </remarks>
        public IDictionary<string, string> Fields { get; private set; } = new Dictionary<string, string>();

        private static bool DetermineIfTransient(HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.InternalServerError || statusCode == HttpStatusCode.RequestTimeout || (int)statusCode == 429;
        }
    }
}
