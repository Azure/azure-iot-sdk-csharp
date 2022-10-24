﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class AmqpConnectionStatusChange
    {
        private readonly string _deviceId;

        public AmqpConnectionStatusChange(string deviceId)
        {
            ConnectionStatusChangeCount = 0;
            _deviceId = deviceId;
        }

        public int ConnectionStatusChangeCount { get; set; }

        public void ConnectionStatusChangeHandler(ConnectionStatusInfo connectionStatusInfo)
        {
            ConnectionStatusChangeCount++;
            VerboseTestLogger.WriteLine($"{nameof(AmqpConnectionStatusChange)}.{nameof(ConnectionStatusChangeHandler)}: {_deviceId}: status={connectionStatusInfo.Status} statusChangeReason={connectionStatusInfo.ChangeReason} count={ConnectionStatusChangeCount}");
        }
    }
}
