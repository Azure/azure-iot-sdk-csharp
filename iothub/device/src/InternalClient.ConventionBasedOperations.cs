// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a convention based device/ module can use to send telemetry to the service,
    /// respond to commands and perform operations on its properties.
    /// </summary>
    internal partial class InternalClient
    {
        internal PayloadConvention PayloadConvention => _clientOptions.PayloadConvention ?? DefaultPayloadConvention.Instance;

        internal Task SendTelemetryAsync(TelemetryMessage telemetryMessage, CancellationToken cancellationToken)
        {
            if (telemetryMessage == null)
            {
                throw new ArgumentNullException(nameof(telemetryMessage));
            }

            if (telemetryMessage.Telemetry != null)
            {
                telemetryMessage.Telemetry.Convention = PayloadConvention;
                telemetryMessage.PayloadContentEncoding = PayloadConvention.PayloadEncoder.ContentEncoding.WebName;
                telemetryMessage.PayloadContentType = PayloadConvention.PayloadSerializer.ContentType;
            }

            return SendEventAsync(telemetryMessage, cancellationToken);
        }
    }
}
