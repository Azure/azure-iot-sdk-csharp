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
            RecommendedAction = RecommendedAction.DefaultAction;
        }

        internal ConnectionInfo(ConnectionStatus status, ConnectionStatusChangeReason changeReason)
        {
            Status = status;
            ChangeReason = changeReason;
            StatusLastChangedOnUtc = DateTimeOffset.UtcNow;
            RecommendedAction = GetRecommendedAction(status, changeReason);
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
        public DateTimeOffset StatusLastChangedOnUtc { get; }

        /// <summary>
        /// Recommended actions for users to take upon different ConnectionStatus and ConnectionStatusChangeReason.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="RecommendedAction.DefaultAction"/>.
        /// </remark>>
        public RecommendedAction RecommendedAction { get; }

        private RecommendedAction GetRecommendedAction(ConnectionStatus status, ConnectionStatusChangeReason changeReason)
        {
            switch (status)
            {
                case ConnectionStatus.Connected:
                    return RecommendedAction.PerformNormally;

                case ConnectionStatus.DisconnectedRetrying:
                case ConnectionStatus.Disabled:
                    return RecommendedAction.NotDoAnything;

                case ConnectionStatus.Disconnected:
                    switch (changeReason)
                    {
                        case ConnectionStatusChangeReason.RetryExpired:
                        case ConnectionStatusChangeReason.CommunicationError:
                            return RecommendedAction.ReinitializeClient;

                        case ConnectionStatusChangeReason.BadCredential:
                        case ConnectionStatusChangeReason.DeviceDisabled:
                            return RecommendedAction.NotDoAnything;

                        default:
                            return RecommendedAction.ContactUs;
                    }

                default:
                    return RecommendedAction.ContactUs;
            }
        }
    }
}
