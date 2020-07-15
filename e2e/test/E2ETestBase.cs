// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class creates an instance of the logger for each test and logs the test result along with other useful information.
    /// </summary>
    public class E2ETestBase
    {
        protected TestLogging Logger { get; set; }
        private Stopwatch Stopwatch { get; set; }
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            Stopwatch = Stopwatch.StartNew();
            Logger = TestLogging.GetInstance(TestContext);
            Logger.WriteLine($"Starting test - {TestContext.TestName}", SeverityLevel.Information);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Stopwatch.Stop();
            double elapsed = Stopwatch.Elapsed.TotalSeconds;
            var properties = new Dictionary<string, string>
            {
                { LoggingPropertyNames.SecondsElapsed, elapsed.ToString() }
            };
            Logger.WriteLine($"Finished test - {TestContext.TestName}", SeverityLevel.Information, properties);
            // As this is not an application that keeps running, explicitly flushing is required to ensure we do not lose any logs.
            // The recommendation from AI is to wait for 5 seconds after flushing.
            Logger.Flush();
            Thread.Sleep(5000);
        }
    }
}
