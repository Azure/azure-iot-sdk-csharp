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
        public const string ClassName = "ClassName";
        public const string TestFramework = "TestFramework";
        public const string TestStatus = "TestStatus";
        public const string SecondsElapsed = "SecondsElapsed";
        public const string Language = "Language";
        public const string Service = "Service";
    }
}
