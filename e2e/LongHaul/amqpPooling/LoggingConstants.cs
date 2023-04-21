// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.LongHual.AmqpPooling
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

        // Event names

        public const string StartingRun = "StartingRun";
        public const string EndingRun = "EndingRun";

        // Metric names

        public const string TotalTelemetryMessagesSent = "TotalTelemetryMessagesSent";
        public const string TelemetryMessageDelaySeconds = "TelemetryMessageDelaySeconds";
    }
}
