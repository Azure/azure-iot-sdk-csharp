// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// The exception that is thrown when an error occurs during device provisioning client operation.
    /// </summary>
    [Serializable]
    public class ProvisioningClientException : Exception
    {
        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        public ProvisioningClientException(string message, bool isTransient)
            : this(message, null, isTransient)
        {
        }

        /// <summary>
        /// Creates a new instance of this class.
        /// </summary>
        /// <param name="message">The exception message.</param>
        /// <param name="isTransient">True if the error is transient.</param>
        /// <param name="innerException">The inner exception.</param>
        public ProvisioningClientException(string message, Exception innerException, bool isTransient)
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
        public ProvisioningClientException(string message, Exception innerException, bool isTransient, int errorCode, string trackingId)
            : base(message, innerException)
        {
            IsTransient = isTransient;
            ErrorCode = errorCode;
            TrackingId = trackingId;
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
    }
}
