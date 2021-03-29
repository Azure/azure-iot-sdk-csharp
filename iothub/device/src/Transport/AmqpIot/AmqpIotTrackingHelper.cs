// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Client.Extensions;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIot
{
    internal static class AmqpIotTrackingHelper
    {
        // TODO: GatewayId is not assigned to anywhere in this class. Likely a bug!
        private static readonly string s_gatewayId = string.Empty;

        public static string GenerateTrackingId()
        {
            return GenerateTrackingId(string.Empty, string.Empty);
        }

        public static string GenerateTrackingId(string backendId, string partitionId)
        {
            string gatewayId = s_gatewayId;
            return GenerateTrackingId(gatewayId, backendId, partitionId);
        }

        public static string GenerateTrackingId(string gatewayId, string backendId, string partitionId)
        {
            string trackingId;
            trackingId = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            if (!string.IsNullOrEmpty(gatewayId))
            {
                gatewayId = "0";
                trackingId = "{0}-G:{1}".FormatInvariant(trackingId, gatewayId);
            }

            if (!string.IsNullOrEmpty(backendId))
            {
                backendId = backendId.Substring(backendId.LastIndexOf("_", StringComparison.InvariantCultureIgnoreCase) + 1);
                trackingId = "{0}-B:{1}".FormatInvariant(trackingId, backendId);
            }

            if (!string.IsNullOrEmpty(partitionId))
            {
                trackingId = "{0}-P:{1}".FormatInvariant(trackingId, partitionId);
            }

            trackingId = "{0}-TimeStamp:{1}".FormatInvariant(trackingId, DateTime.UtcNow);
            return trackingId;
        }

        public static string GenerateTrackingId(this AmqpException exception)
        {
            return exception.GenerateTrackingId(s_gatewayId, string.Empty, string.Empty);
        }

        public static string GenerateTrackingId(this AmqpException exception, string backendId, string partitionId)
        {
            return exception.GenerateTrackingId(s_gatewayId, backendId, partitionId);
        }

        public static string GenerateTrackingId(this AmqpException exception, string gatewayId, string backendId, string partitionId)
        {
            if (exception.Error.Info == null)
            {
                exception.Error.Info = new Fields();
            }

            if (!exception.Error.Info.Any() || !exception.Error.Info.TryGetValue(AmqpIotConstants.TrackingId, out string trackingId))
            {
                trackingId = GenerateTrackingId(gatewayId, backendId, partitionId);
                exception.Error.Info.Add(AmqpIotConstants.TrackingId, trackingId);
            }
            return trackingId;
        }

        public static string CheckAndAddGatewayIdToTrackingId(string gatewayId, string trackingId)
        {
            if (!string.IsNullOrEmpty(trackingId) && trackingId.IndexOf("-B:", StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                return "{0}-G:{1}{2}".FormatInvariant(
                    trackingId.Substring(0, trackingId.IndexOf("-B:", StringComparison.InvariantCultureIgnoreCase)),
                    gatewayId,
                    trackingId.Substring(trackingId.IndexOf("-B:", StringComparison.InvariantCultureIgnoreCase) + 3));
            }
            else
            {
                return GenerateTrackingId(gatewayId, string.Empty, string.Empty);
            }
        }

        public static string GetTrackingId(this AmqpException amqpException)
        {
            Error errorObj = amqpException.Error;
            string trackingId = null;
            if (errorObj.Info != null)
            {
                errorObj.Info.TryGetValue(AmqpIotConstants.TrackingId, out trackingId);
            }
            return trackingId;
        }

        public static string GetGatewayId(this AmqpLink link)
        {
            string gatewayId = null;

            if (link.Settings.LinkName.IndexOf("_G:", StringComparison.InvariantCultureIgnoreCase) > 0)
            {
                gatewayId = link.Settings.LinkName.Substring(link.Settings.LinkName.IndexOf("_G:", StringComparison.InvariantCultureIgnoreCase) + 3);
            }
            return gatewayId;
        }
    }
}
