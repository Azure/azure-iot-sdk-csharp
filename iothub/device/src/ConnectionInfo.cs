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
            Status = ConnectionStatus.Disabled;
            ChangeReason = ConnectionStatusChangeReason.ClientClose;
            StatusLastChangedOnUtc = DateTimeOffset.UtcNow;
            RecommendedAction = RecommendedAction.DisposeAndOpenIfWish;
        }

        internal ConnectionInfo(ConnectionStatus status, ConnectionStatusChangeReason changeReason, DateTimeOffset changedOnUtc)
        {
            Status = status;
            ChangeReason = changeReason;
            StatusLastChangedOnUtc = changedOnUtc;
            RecommendedAction = GetRecommendedActionBasedOnConnectionStatusAndChangeReason(status, changeReason);
        }

        /// <summary>
        /// The current connection status of the device or module.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="ConnectionStatus.Disabled"/>.
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

        /// <summary>
        /// Recommended actions for users to take upon different ConnectionStatus and ConnectionStatusChangeReason.
        /// </summary>
        /// <remark>
        /// Defaults to <see cref="RecommendedAction.DisposeAndOpenIfWish"/>.
        /// </remark>>
        public RecommendedAction RecommendedAction { get; internal set; }

        private RecommendedAction GetRecommendedActionBasedOnConnectionStatusAndChangeReason(ConnectionStatus status, ConnectionStatusChangeReason changeReason)
        {
            switch (status)
            {
                case ConnectionStatus.Connected:
                    return RecommendedAction.NoActionWhenNormal;

                case ConnectionStatus.DisconnectedRetrying:
                    return RecommendedAction.NoActionWhenRetrying;

                case ConnectionStatus.Disabled:
                    return RecommendedAction.DisposeAndOpenIfWish;

                case ConnectionStatus.Disconnected:
                    switch (changeReason)
                    {
                        case ConnectionStatusChangeReason.RetryExpired:
                            return RecommendedAction.DisposeAndOpenIfWish;

                        case ConnectionStatusChangeReason.CommunicationError:
                            return RecommendedAction.DisposeAndOpenIfWish;

                        case ConnectionStatusChangeReason.BadCredential:
                            return RecommendedAction.UseValidCredential;

                        case ConnectionStatusChangeReason.DeviceDisabled:
                            return RecommendedAction.FixDeviceStatus;

                        default:
                            return RecommendedAction.ContactUs;
                    }

                default:
                    return RecommendedAction.ContactUs;
            }
        }
    }
}
