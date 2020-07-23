// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// TODO: Uncomment this once all tests inherit from E2EMsTestBase
//Workers = 0 makes the test engine use one worker per available core. It does not mean to run serially.
//[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]
namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class creates an instance of the logger for each test and logs the test result along with other useful information.
    /// </summary>
    public class E2EMsTestBase
    {
        private const string TestStartedEventName = "TestStarted";
        private const string TestFinishedEventName = "TestFinished";

        protected MsTestLogger Logger { get; set; }
        private Stopwatch Stopwatch { get; set; }
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            Stopwatch = Stopwatch.StartNew();
            Logger = MsTestLogger.GetInstance(TestContext);
            Logger.Trace($"Starting test - {TestContext.TestName}", SeverityLevel.Information);
            Logger.Event(TestStartedEventName);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            Stopwatch.Stop();

            var extraProperties = new Dictionary<string, string>
            {
                { LoggingPropertyNames.TimeElapsed, Stopwatch.Elapsed.ToString() },
                { LoggingPropertyNames.TestStatus, TestContext.CurrentTestOutcome.ToString() },
            };

            Logger.Trace($"Finished test - {TestContext.TestName}", SeverityLevel.Information, extraProperties);
            Logger.Event(TestFinishedEventName, extraProperties);
            // As this is not an application that keeps running, explicitly flushing is required to ensure we do not lose any logs.
            Logger.SafeFlush();
        }
    }
}
