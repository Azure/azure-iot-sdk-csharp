// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// The property names to log to telemetry.
    /// </summary>
    public class LoggingPropertyNames
    {
        public const string TestRunId = "TestRunId";
        public const string TestName = "TestName";
        public const string TestClassName = "TestClassName";
        public const string TargetFramework = "TargetFramework";
        public const string TestStatus = "TestStatus";
        public const string TimeElapsed = "TimeElapsed";
        public const string SdkLanguage = "SdkLanguage";
        public const string Service = "Service";
        public const string Caller = "Caller";
        public const string BuildId = "BuildId";
        public const string TestFailureReason = "TestResult";
        public const string OsInfo = "OsInfo";
    }
}
