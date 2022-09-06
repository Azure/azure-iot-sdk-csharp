// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// /// NOTE: USE THIS IN TESTS
    /// Ms Test framework specific logging.
    /// Every test will have its own instance of this logger with properties specific to the test.
    /// This logger uses the global logger to write logs but has its own set of properties.
    /// We cannot inherit and log using this because this logger object gets disposed at the end of the test
    /// and we will lose logs without doing a flush. Flush is expensive and should be done only once when
    /// we exit the test suite.
    /// </summary>
    public class MsTestLogger
    {
        // Test specific properties that cannot be changed.
        private readonly Dictionary<string, string> MsTestProperties = new();

        // This property bag can be used to add other properties.
        public Dictionary<string, string> Properties { get; } = new();

        internal MsTestLogger(TestContext testContext)
        {
            // Framework against which the test is running.
            var targetFramework = (TargetFrameworkAttribute)Assembly
                .GetExecutingAssembly()
                .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                .SingleOrDefault();

            string operatingSystem = RuntimeInformation.OSDescription.Trim();

            // Add test related properties.
            MsTestProperties.Add(LoggingPropertyNames.TestName, testContext.TestName);
            MsTestProperties.Add(LoggingPropertyNames.TestClassName, testContext.FullyQualifiedTestClassName);
            MsTestProperties.Add(LoggingPropertyNames.TargetFramework, targetFramework.FrameworkName);
            MsTestProperties.Add(LoggingPropertyNames.OsInfo, operatingSystem);
        }

        public void Trace(string message, SeverityLevel severity = SeverityLevel.Information, IDictionary<string, string> extraProperties = null)
        {
            IDictionary<string, string>[] bagsToMerge = new[] { MsTestProperties, Properties, extraProperties };
            TestLogger.Instance.Trace(message, severity, TestLogger.MergePropertyBags(bagsToMerge));
        }
    }
}
