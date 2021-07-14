// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Common
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.Linq;

    using Microsoft.Azure.Amqp;
    using Microsoft.Azure.Amqp.Framing;
    using Microsoft.Azure.Devices.Common.Client;
    using Microsoft.Azure.Devices.Common.Exceptions;

    /// <summary>
    /// Generates tracking Id with gateway, backend, partition and timestamp data.
    /// </summary>
    /// <remarks>
    /// Tracking Id format is [GUID][-G:GatewayId][-B:BackendId][-P:PartitionId][-TimeStamp:Timestamp]
    /// </remarks>
    [Obsolete("This is for internal use only. SDK will not support this for external usage.")]
    public static class TrackingHelper
    {
        /// <summary>
        /// Gateway Id.
        /// </summary>
        [SuppressMessage("Usage", "CA2211:Non-constant fields should not be visible", Justification = "This property may be used by others so it is not safe to modify it.")]
        public static string GatewayId;

        private const string GatewayPrefix = "-G:";
        private const string BackendPrefix = "-B:";
        private const string PartitionPrefix = "-P:";
        private const string TimeStampPrefix = "-TimeStamp:";

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GenerateTrackingId()
        {
            return GenerateTrackingId(string.Empty, string.Empty);
        }

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GenerateTrackingId(string backendId, string partitionId)
        {
            string gatewayId = GatewayId;
            return GenerateTrackingId(gatewayId, backendId, partitionId);
        }

        /// <summary>
        /// Generates a unique tracking Id with gateway, backend, partition and timestamp information.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        /// <remarks>
        /// Tracking Id format is [GUID][-G:GatewayId][-B:BackendId][-P:PartitionId][-TimeStamp:Timestamp]
        /// </remarks>
        public static string GenerateTrackingId(string gatewayId, string backendId, string partitionId)
        {
            string trackingId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);

            if (!string.IsNullOrEmpty(gatewayId))
            {
                if (gatewayId.IndexOf(".", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    gatewayId = gatewayId.Substring(gatewayId.LastIndexOf(".", StringComparison.OrdinalIgnoreCase) + 1);
                }
                else
                {
                    gatewayId = "0";
                }

                trackingId = "{0}{1}{2}".FormatInvariant(trackingId, GatewayPrefix, gatewayId);
            }

            if (!string.IsNullOrEmpty(backendId))
            {
                if (backendId.IndexOf(".", StringComparison.InvariantCultureIgnoreCase) > 0)
                {
                    backendId = backendId.Substring(backendId.LastIndexOf(".", StringComparison.OrdinalIgnoreCase) + 1);
                }

                trackingId = "{0}{1}{2}".FormatInvariant(trackingId, BackendPrefix, backendId);
            }

            if (!string.IsNullOrEmpty(partitionId))
            {
                trackingId = "{0}{1}{2}".FormatInvariant(trackingId, PartitionPrefix, partitionId);
            }

            trackingId = "{0}{1}{2}".FormatInvariant(trackingId, TimeStampPrefix, DateTime.UtcNow);
            return trackingId;
        }

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GenerateTrackingId(this AmqpException exception)
        {
            return exception.GenerateTrackingId(TrackingHelper.GatewayId, string.Empty, string.Empty);
        }

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GenerateTrackingId(this AmqpException exception, string backendId, string partitionId)
        {
            return exception.GenerateTrackingId(TrackingHelper.GatewayId, backendId, partitionId);
        }

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GenerateTrackingId(this AmqpException exception, string gatewayId, string backendId, string partitionId)
        {
            if (exception.Error.Info == null)
            {
                exception.Error.Info = new Fields();
            }

            string trackingId;
            if (!exception.Error.Info.Any() || !exception.Error.Info.TryGetValue(IotHubAmqpProperty.TrackingId, out trackingId))
            {
                trackingId = GenerateTrackingId(gatewayId, backendId, partitionId);
                exception.Error.Info.Add(IotHubAmqpProperty.TrackingId, trackingId);
            }
            return trackingId;
        }

        /// <summary>
        /// Sets error code from AMQP exception. Not used.
        /// </summary>
        public static void SetErrorCode(this AmqpException exception)
        {
            if (exception.Error.Info == null)
            {
                exception.Error.Info = new Fields();
            }

            ErrorCode errorCode;
            if (!exception.Error.Info.TryGetValue(IotHubAmqpProperty.TrackingId, out errorCode))
            {
                exception.Error.Info.Add(IotHubAmqpProperty.ErrorCode, GetErrorCodeFromAmqpError(exception.Error));
            }
        }

        /// <summary>
        /// Generates a tracking Id with gateway details if not already there in the tracking Id.
        /// </summary>
        /// <param name="trackingId">Tracking Id.</param>
        /// <returns>A tracking Id with gateway details.</returns>
        public static string CheckAndAddGatewayIdToTrackingId(string trackingId)
        {
            if (!string.IsNullOrEmpty(trackingId)
                && !(trackingId.IndexOf(GatewayPrefix, StringComparison.InvariantCultureIgnoreCase) > 0)
                && trackingId.IndexOf(BackendPrefix, StringComparison.InvariantCultureIgnoreCase) > 0
                && GatewayId != null)
            {
                int indexOfBackend = trackingId.IndexOf(BackendPrefix, StringComparison.Ordinal);
                return "{0}{3}{1}{2}".FormatInvariant(
                    trackingId.Substring(0, indexOfBackend),
                    GatewayId,
                    trackingId.Substring(indexOfBackend),
                    GatewayPrefix);
            }
            else
            {
                return GenerateTrackingId(GatewayId, string.Empty, string.Empty);
            }
        }

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GetTrackingId(this AmqpException amqpException)
        {
            Error errorObj = amqpException.Error;
            string trackingId = null;
            if (errorObj.Info != null)
            {
                errorObj.Info.TryGetValue(IotHubAmqpProperty.TrackingId, out trackingId);
            }
            return trackingId;
        }

        /// <summary>
        /// Generates a tracking Id. Not used.
        /// </summary>
        /// <returns>A tracking Id.</returns>
        public static string GetGatewayId(this AmqpLink link)
        {
            string gatewayId = null;
            if (link.Settings.LinkName.IndexOf("_G:", StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                gatewayId = link.Settings.LinkName.Substring(link.Settings.LinkName.IndexOf("_G:", StringComparison.Ordinal) + 3);
            }
            return gatewayId;
        }

        /// <summary>
        /// Gets AMQP error code. Not used.
        /// </summary>
        /// <returns>Error code.</returns>
        public static ErrorCode GetErrorCodeFromAmqpError(Error ex)
        {
            if (ex == null)
            {
                throw new ArgumentNullException(nameof(ex), "The Error property of the AMQP exception is null.");
            }

            if (ex.Condition.Equals(AmqpErrorCode.NotFound))
            {
                return ErrorCode.DeviceNotFound;
            }
            if (ex.Condition.Equals(IotHubAmqpErrorCode.IotHubSuspended))
            {
                return ErrorCode.IotHubSuspended;
            }
            if (ex.Condition.Equals(IotHubAmqpErrorCode.IotHubNotFoundError))
            {
                return ErrorCode.IotHubNotFound;
            }
            if (ex.Condition.Equals(IotHubAmqpErrorCode.PreconditionFailed))
            {
                return ErrorCode.PreconditionFailed;
            }
            if (ex.Condition.Equals(AmqpErrorCode.MessageSizeExceeded))
            {
                return ErrorCode.MessageTooLarge;
            }
            if (ex.Condition.Equals(AmqpErrorCode.ResourceLimitExceeded))
            {
                return ErrorCode.DeviceMaximumQueueDepthExceeded;
            }
            if (ex.Condition.Equals(AmqpErrorCode.UnauthorizedAccess))
            {
                return ErrorCode.IotHubUnauthorizedAccess;
            }

            return ErrorCode.InvalidErrorCode;
        }
    }
}
