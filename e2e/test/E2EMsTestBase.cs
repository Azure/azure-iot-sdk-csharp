// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.VisualStudio.TestTools.UnitTesting;

//Workers = 0 makes the test engine use one worker per available core. It does not mean to run serially.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class creates an instance of the logger for each test and logs the test result along with other useful information.
    /// </summary>
    public class E2EMsTestBase : IDisposable
    {
        private ConsoleEventListener _listener;
        private Stopwatch _stopwatch;

        // Test specific logger instance
        protected MsTestLogger Logger { get; set; }

        public TestContext TestContext { get; set; }

        // The test timeout for typical e2e tests
        protected const int TestTimeoutMilliseconds = 3 * 60 * 1000; // 3 minutes

        // The test timeout for long running e2e tests
        protected const int LongRunningTestTimeoutMilliseconds = 5 * 60 * 1000; // 5 minutes

        // The test timeout for long running e2e tests the inspect the connection status change logic on disabling a device.
        protected const int ConnectionStatusChangeTestTimeoutMilliseconds = 10 * 60 * 1000; // 10 minutes

        // The test timeout for e2e tests that involve testing token refresh
        protected const int TokenRefreshTestTimeoutMilliseconds = 20 * 60 * 1000; // 20 minutes

        [TestInitialize]
        public void TestInitialize()
        {
            _stopwatch = Stopwatch.StartNew();
            Logger = new MsTestLogger(TestContext);

            // Note: Events take long and increase run time of the test suite, so only using trace.
            Logger.Trace($"Starting test - {TestContext.TestName}", SeverityLevel.Information);

            _listener = new ConsoleEventListener();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _stopwatch.Stop();

            var extraProperties = new Dictionary<string, string>
            {
                { LoggingPropertyNames.TimeElapsed, _stopwatch.Elapsed.ToString() },
                { LoggingPropertyNames.TestStatus, TestContext.CurrentTestOutcome.ToString() },
            };

            // Note: Events take long and increase run time of the test suite, so only using trace.
            Logger.Trace($"Finished test - {TestContext.TestName}", SeverityLevel.Information, extraProperties);

            // Dispose the managed resources, so that each test run starts with a fresh slate.
            Dispose();
        }

        [AssemblyCleanup]
        public static async Task AssemblyCleanup()
        {
            // Flush before the test suite ends to ensure we do not lose any logs.
            await TestLogger.Instance.SafeFlushAsync().ConfigureAwait(false);
        }

        public void Dispose()
        {
            // Dispose managed resources
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _listener.Dispose();
            }
        }
    }
}
