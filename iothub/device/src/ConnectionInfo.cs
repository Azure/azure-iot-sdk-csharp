using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The connection information since the last state change.
    /// </summary>
    public class ConnectionInfo
    {
        internal ConnectionInfo() { }

        internal ConnectionInfo(ConnectionState state, ConnectionStateChangeReason changeReason, DateTimeOffset changedOnUtc)
        { 
            State = state;
            ChangeReason = changeReason;
            ChangedOnUtc = changedOnUtc;
        }

        /// <summary>
        /// The current connection state of the device or module.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionState.Disconnected"/>.
        /// </remark>>
        public ConnectionState State { get; internal set; } = ConnectionState.Disconnected;

        /// <summary>
        /// The reason for the current connection state change.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionStateChangeReason.ClientClose"/>.
        /// </remark>
        public ConnectionStateChangeReason ChangeReason { get; internal set; } = ConnectionStateChangeReason.ClientClose;

        /// <summary>
        /// Timestamp in UTC when the last connection state was changed.
        /// </summary>
        public DateTimeOffset ChangedOnUtc { get; internal set; } = DateTime.UtcNow;
    }
}
