// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The connection status information since the last status change.
    /// </summary>
    public class ConnectionStatusInfo
    {
        internal ConnectionStatusInfo()
            : this(ConnectionStatus.Disconnected, ConnectionStatusChangeReason.ClientClosed)
        {
            RecommendedAction = RecommendedAction.OpenConnection;
        }

        internal ConnectionStatusInfo(ConnectionStatus status, ConnectionStatusChangeReason changeReason)
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
        /// Defaults to <see cref="ConnectionStatusChangeReason.ClientClosed"/>.
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
        /// Defaults to <see cref="RecommendedAction.OpenConnection"/>.
        /// </remark>>
        public RecommendedAction RecommendedAction { get; }

        /// Please refer to the <see href="https://github.com/Azure/azure-iot-sdk-csharp/blob/previews/v2/iothub/device/samples/DeviceReconnectionSample/DeviceReconnectionSample.cs">
        /// DeviceReconnectionSample</see> for more details regarding how to use RecommendedAction.
        private static RecommendedAction GetRecommendedAction(ConnectionStatus status, ConnectionStatusChangeReason changeReason)
        {
            switch (status)
            {
                case ConnectionStatus.Connected:
                    return RecommendedAction.PerformNormally;

                case ConnectionStatus.DisconnectedRetrying:
                    return RecommendedAction.WaitForRetryPolicy;

                case ConnectionStatus.Closed:
                    return RecommendedAction.Quit;

                case ConnectionStatus.Disconnected:
                    switch (changeReason)
                    {
                        case ConnectionStatusChangeReason.RetryExpired:
                        case ConnectionStatusChangeReason.CommunicationError:
                            return RecommendedAction.OpenConnection;

                        case ConnectionStatusChangeReason.BadCredential:
                        case ConnectionStatusChangeReason.DeviceDisabled:
                            return RecommendedAction.Quit;
                    }
                    break;
            }

            return RecommendedAction.OpenConnection;
        }
    }
}
