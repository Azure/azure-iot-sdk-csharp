// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
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
    /// Custom implementation to access and log the test failure exceptions.
    /// </summary>
    public class LoggedTestMethod : TestMethodAttribute
    {
        public override TestResult[] Execute(ITestMethod testMethod)
        {
            TestResult[] results = base.Execute(testMethod);
            string testFailureReason = results.First().TestFailureException?.Message;

            // Log only if there is an exception in the test run.
            if (!string.IsNullOrWhiteSpace(testFailureReason))
            {
                // Framework against which the test is running.
                var targetFramework = (TargetFrameworkAttribute)Assembly
                        .GetExecutingAssembly()
                        .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                        .SingleOrDefault();

                string operatingSystem = RuntimeInformation.OSDescription.Trim();

                // Add test related properties.
                var extraProperties = new Dictionary<string, string>
                {
                    { LoggingPropertyNames.TestName, testMethod.TestMethodName },
                    { LoggingPropertyNames.TestClassName, testMethod.TestClassName },
                    { LoggingPropertyNames.TestFailureReason, testFailureReason },
                    { LoggingPropertyNames.TargetFramework, targetFramework.FrameworkName },
                    { LoggingPropertyNames.OsInfo, operatingSystem },
                };

                // Note: Events take long and increase run time of the test suite, so only using trace.
                TestLogger.Instance.Trace($"Test {testMethod.TestMethodName} failed with error {testFailureReason}.", SeverityLevel.Error, extraProperties);
            }
            return results;
        }
    }
}
