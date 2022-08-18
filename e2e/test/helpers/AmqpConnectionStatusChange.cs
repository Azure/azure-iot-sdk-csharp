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
            ConnectionStatusChangeCount = 0;
            _deviceId = deviceId;
            _logger = logger;
        }

        public int ConnectionStatusChangeCount { get; set; }

        public void ConnectionStatusChangeHandler(ConnectionInfo connectionInfo)
        {
            ConnectionStatusChangeCount++;
            _logger.Trace($"{nameof(AmqpConnectionStatusChange)}.{nameof(ConnectionStatusChangeHandler)}: {_deviceId}: status={connectionInfo.Status} statusChangeReason={connectionInfo.ChangeReason} count={ConnectionStatusChangeCount}");
        }
    }
}
