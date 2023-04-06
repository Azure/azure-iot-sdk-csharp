// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.LongHaul.Device
{
    internal static class LoggingConstants
    {
        // Metrics

        public const string DisconnectedDurationSeconds = "DisconnectedDurationSeconds";
        public const string TotalTelemetryMessagesSent = "TotalTelemetryMessagesSent";
        public const string TelemetryMessageDelaySeconds = "TelemetryMessageDelaySeconds";
        public const string MessageBacklog = "MessageBacklog";
        public const string C2dDirectMethodDelaySeconds = "C2dDirectMethodDelaySeconds";
        public const string TotalTwinUpdatesReported = "TotalTwinUpdatesReported";
        public const string TotalTwinCallbacksHandled = "TotalTwinCallbacksHandled";
        public const string TotalDesiredPropertiesHandled = "TotalDesiredPropertiesHandled";
        public const string TotalC2dMessagesCompleted = "TotalC2dMessagesCompleted";
        public const string TotalC2dMessagesRejected = "TotalC2dMessagesRejected";
        public const string C2dMessageDelaySeconds = "C2dMessageDelaySeconds";

        // Events

        public const string StartingRun = "StartingRun";
        public const string ConnectedEvent = "Connected";
        public const string DiscconnectedEvent = "Disconnected";

        // Logging properties

        public const string TestClient = "testClient";
        public const string RunId = "runId";
        public const string SdkLanguage = "sdkLanguage";
        public const string SdkVersion = "sdkVersion";

        public const string Hub = "hub";
        public const string DeviceId = "deviceId";
        public const string Transport = "transport";

        public const string ConnectionReason = "connectionReason";
        public const string ConnectionRecommendedAction = "connectionRecommendedAction";

        public const string DisconnectedStatus = "disconnectedStatus";
        public const string DisconnectedReason = "disconnectedReason";
        public const string DisconnectedRecommendedAction = "disconnectedRecommendedAction";
        public const string ConnectionStatusChangeCount = "connectionStatusChangeCount";
    }
}
