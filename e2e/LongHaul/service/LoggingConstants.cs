﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal static class LoggingConstants
    {
        // Logging properties

        public const string TestClient = "testClient";
        public const string RunId = "runId";
        public const string SdkLanguage = "sdkLanguage";
        public const string SdkVersion = "sdkVersion";

        public const string Hub = "hub";
        public const string Transport = "transport";

        // Events

        public const string StartingRun = "StartingRun";

        // Metrics

        public const string D2cDirectMethodDelaySeconds = "D2cDirectMethodDelaySeconds";
        public const string TotalDirectMethodCallsCount = "TotalDirectMethodCallsCount";
        public const string TotalDesiredPropertiesUpdatesCount = "TotalDesiredPropertiesUpdatesCount";
        public const string TotalC2dMessagesSentCount = "TotalC2dMessagesSentCount";
    }
}
