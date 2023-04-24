// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.LongHaul.AmqpPooling
{
    internal class LoggingConstants
    {
        // Logging properties

        public const string TestType = "testType";
        public const string SdkLanguage = "sdkLanguage";
        public const string DeviceSdkVersion = "deviceSdkVersion";
        public const string ServiceSdkVersion = "serviceSdkVersion";
        public const string Component = "component";

        public const string Hub = "hub";
        public const string DeviceTransportSettings = "deviceTransportSettings";

        public const string ConnectionReason = "connectionReason";
        public const string ConnectionRecommendedAction = "connectionRecommendedAction";
        public const string DisconnectedStatus = "disconnectedStatus";
        public const string DisconnectedReason = "disconnectedReason";
        public const string DisconnectedRecommendedAction = "disconnectedRecommendedAction";
        public const string ConnectionStatusChangeCount = "connectionStatusChangeCount";

        // Event names

        public const string StartingRun = "StartingRun";
        public const string ConnectedEvent = "Connected";
        public const string DiscconnectedEvent = "Disconnected";
        public const string EndingRun = "EndingRun";

        // Metric names

        public const string TotalTelemetryMessagesSent = "TotalTelemetryMessagesSent";
        public const string TelemetryMessageDelaySeconds = "TelemetryMessageDelaySeconds";
        public const string DisconnectedDurationSeconds = "DisconnectedDurationSeconds";
    }
}
