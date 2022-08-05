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
            LastConnectionStateChangesReason = null;
            ConnectionStateChangesCount = 0;
            _deviceId = deviceId;
            _logger = logger;
        }

        public int ConnectionStateChangesCount { get; set; }

        public ConnectionState? LastConnectionState { get; set; }

        public ConnectionStateChangesReason? LastConnectionStateChangesReason { get; set; }

        public void ConnectionStateChangesHandler(ConnectionState state, ConnectionStateChangesReason reason)
        {
            ConnectionStateChangesCount++;
            LastConnectionState = state;
            LastConnectionStateChangesReason = reason;
            _logger.Trace($"{nameof(AmqpConnectionStateChange)}.{nameof(ConnectionStateChangesHandler)}: {_deviceId}: state={state} stateChangesReason={reason} count={ConnectionStateChangesCount}");
        }
    }
}
