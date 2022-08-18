// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The connection information since the last status change.
    /// </summary>
    public class ConnectionInfo
    {
        internal ConnectionInfo()
        {
            Status = ConnectionStatus.Disconnected;
            ChangeReason = ConnectionStatusChangeReason.ClientClose;
            StatusLastChangedOnUtc = DateTimeOffset.UtcNow;
        }

        internal ConnectionInfo(ConnectionStatus status, ConnectionStatusChangeReason changeReason, DateTimeOffset changedOnUtc)
        { 
            Status = status;
            ChangeReason = changeReason;
            StatusLastChangedOnUtc = changedOnUtc;
        }

        /// <summary>
        /// The current connection status of the device or module.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionStatus.Disconnected"/>.
        /// </remark>>
        public ConnectionStatus Status { get; }

        /// <summary>
        /// The reason for the current connection status change.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionStatusChangeReason.ClientClose"/>.
        /// </remark>
        public ConnectionStatusChangeReason ChangeReason { get; }

        /// <summary>
        /// Timestamp in UTC when the last connection status was changed.
        /// </summary>
        public DateTimeOffset StatusLastChangedOnUtc { get; internal set; }
    }
}
