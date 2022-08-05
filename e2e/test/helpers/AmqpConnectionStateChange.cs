// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class AmqpConnectionStateChange
    {
        private readonly string _deviceId;
        private readonly MsTestLogger _logger;

        public AmqpConnectionStateChange(string deviceId, MsTestLogger logger)
        {
            LastConnectionState = null;
            LastConnectionStateChangeReason = null;
            ConnectionStateChangeCount = 0;
            _deviceId = deviceId;
            _logger = logger;
        }

        public int ConnectionStateChangeCount { get; set; }

        public ConnectionState? LastConnectionState { get; set; }

        public ConnectionStateChangeReason? LastConnectionStateChangeReason { get; set; }

        public void ConnectionStateChangeHandler(ConnectionState state, ConnectionStateChangeReason reason)
        {
            ConnectionStateChangeCount++;
            LastConnectionState = state;
            LastConnectionStateChangeReason = reason;
            _logger.Trace($"{nameof(AmqpConnectionStateChange)}.{nameof(ConnectionStateChangeHandler)}: {_deviceId}: state={state} stateChangeReason={reason} count={ConnectionStateChangeCount}");
        }
    }
}
