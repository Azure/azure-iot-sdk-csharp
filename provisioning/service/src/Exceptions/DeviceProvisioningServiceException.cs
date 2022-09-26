// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service exceptions on the Service Client.
    /// </summary>
    public class DeviceProvisioningServiceException : Exception
    {
        /// <summary>
        /// Initializes the <see cref="DeviceProvisioningServiceException"/> exception type.
        /// </summary>
        /// <param name="message">The message.</param>
        public DeviceProvisioningServiceException(string message)
            : this(message, innerException: null, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes the <see cref="DeviceProvisioningServiceException"/> exception type.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="isTransient">True if the error is transient and the operation should be retried.</param>
        public DeviceProvisioningServiceException(string message, bool isTransient)
            : this(message, innerException: null, isTransient: isTransient)
        {
        }

        /// <summary>
        /// Initializes the <see cref="DeviceProvisioningServiceException"/> exception type.
        /// </summary>
        /// <param name="innerException">The inner exception.</param>
        public DeviceProvisioningServiceException(Exception innerException)
            : this(string.Empty, innerException, isTransient: false)
        {
        }

        /// <summary>
        /// Initializes the <see cref="DeviceProvisioningServiceException"/> exception type.
        /// </summary>
        /// <param name="message">The message</param>
        /// <param name="innerException">The inner exception</param>
        /// <param name="isTransient">True if the error is transient and the operation should be retried.</param>
        internal DeviceProvisioningServiceException(string message, Exception innerException, bool isTransient)
            : base(message, innerException)
        {
            IsTransient = isTransient;
        }

        /// <summary>
        /// Initializes the <see cref="DeviceProvisioningServiceException"/> exception type.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="statusCode">The 3-digit HTTP status code returned by Device Provisioning Service.</param>
        /// <param name="fields">The HTTP headers.</param>
        internal DeviceProvisioningServiceException(string message, HttpStatusCode statusCode, IDictionary<string, string> fields)
            : base(message)
        {
            IsTransient = DetermineIfTransient(statusCode);
            StatusCode = statusCode;
            Fields = fields;
        }

        /// <summary>
        /// Initializes the <see cref="DeviceProvisioningServiceException"/> exception type.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="statusCode">The 3-digit HTTP status code returned by Device Provisioning Service.</param>
        /// <param name="errorCode">The specific 6-digit error code in the DPS response, if available.</param>
        /// <param name="trackingId">Service reported tracking Id. Use this when reporting a service issue.</param>
        /// <param name="fields">The HTTP headers.</param>
        internal DeviceProvisioningServiceException(string message, HttpStatusCode statusCode, int errorCode, string trackingId, IDictionary<string, string> fields)
            : base(message)
        {
            IsTransient = DetermineIfTransient(statusCode);
            StatusCode = statusCode;
            ErrorCode = errorCode;
            TrackingId = trackingId;
            Fields = fields;
        }

        /// <summary>
        /// True if the error is transient.
        /// </summary>
        public bool IsTransient { get; private set; }

        /// <summary>
        /// Service reported tracking Id. Use this when reporting a service issue.
        /// </summary>
        public string TrackingId { get; private set; }

        /// <summary>
        /// The 3-digit HTTP status code returned by Device Provisioning Service.
        /// </summary>
        public HttpStatusCode StatusCode { get; private set; }

        /// <summary>
        /// The specific 6-digit error code in the DPS response, if available.
        /// </summary>
        public int ErrorCode { get; private set; }

        /// <summary>
        /// The HTTP headers.
        /// </summary>
        /// <remarks>
        /// This is used by DPS E2E tests.
        /// </remarks>
        public IDictionary<string, string> Fields { get; private set; }

        private bool DetermineIfTransient(HttpStatusCode statusCode)
        {
            return statusCode >= HttpStatusCode.InternalServerError || (int)statusCode == 429;
        }
    }
}
