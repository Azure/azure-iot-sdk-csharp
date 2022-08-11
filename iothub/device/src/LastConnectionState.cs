using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// 
    /// </summary>
    public class LastConnectionState
    {
        /// <summary>
        /// Connection state supported by the client.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionState.Disconnected"/>.
        /// </remark>>
        public ConnectionState State { get; set; } = ConnectionState.Disconnected;

        /// <summary>
        /// Connection state change reason supported by the client.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionStateChangeReason.ClientClose"/>.
        /// </remark>
        public ConnectionStateChangeReason ChangeReason { get; set; } = ConnectionStateChangeReason.ClientClose;

        /// <summary>
        /// Timestamp in UTC when the last connection state was changed.
        /// </summary>
        public DateTime ChangedDateTimeUtc { get; set; }
    }
}
