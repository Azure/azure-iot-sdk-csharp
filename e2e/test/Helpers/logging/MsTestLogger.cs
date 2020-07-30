// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// Ms Test framework specific logging.
    /// </summary>
    public class MsTestLogger : TestLogger
    {
        internal MsTestLogger(TestContext testContext) : base()
        {
            // Framework against which the test is running.
            var targetFramework = (TargetFrameworkAttribute)Assembly
                    .GetExecutingAssembly()
                    .GetCustomAttributes(typeof(TargetFrameworkAttribute), false)
                    .SingleOrDefault();

            string operatingSystem = RuntimeInformation.OSDescription.Trim();

            // Add test related properties.
            Properties.Add(LoggingPropertyNames.TestName, testContext.TestName);
            Properties.Add(LoggingPropertyNames.TestClassName, testContext.FullyQualifiedTestClassName);
            Properties.Add(LoggingPropertyNames.TargetFramework, targetFramework.FrameworkName);
            Properties.Add(LoggingPropertyNames.OsInfo, operatingSystem);
        }
    }
}
