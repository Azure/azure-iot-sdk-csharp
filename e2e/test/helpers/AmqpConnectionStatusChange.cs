// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class AmqpConnectionStatusChange
    {
        private readonly string _deviceId;
        private readonly MsTestLogger _logger;

        public AmqpConnectionStatusChange(string deviceId, MsTestLogger logger)
        {
            LastConnectionStatus = null;
            LastConnectionStatusChangeReason = null;
            ConnectionStatusChangeCount = 0;
            _deviceId = deviceId;
            _logger = logger;
        }

        public int ConnectionStatusChangeCount { get; set; }

        public ConnectionStatus? LastConnectionStatus { get; set; }

        public ConnectionStatusChangeReason? LastConnectionStatusChangeReason { get; set; }

        public void ConnectionStatusChangesHandler(ConnectionStatus status, ConnectionStatusChangeReason reason)
        {
            ConnectionStatusChangeCount++;
            LastConnectionStatus = status;
            LastConnectionStatusChangeReason = reason;
            _logger.Trace($"{nameof(AmqpConnectionStatusChange)}.{nameof(ConnectionStatusChangesHandler)}: {_deviceId}: status={status} statusChangeReason={reason} count={ConnectionStatusChangeCount}");
        }
    }
}
