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
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.MethodLevel)]

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class creates an instance of the logger for each test and logs the test result along with other useful information.
    /// </summary>
    public class E2EMsTestBase : IDisposable
    {
        private const string TestStartedEventName = "TestStarted";
        private const string TestFinishedEventName = "TestFinished";

        private static readonly string[] s_eventProviders = new string[] { "DotNetty-Default", "Microsoft-Azure-", };
        private ConsoleEventListener _listener;

        protected MsTestLogger Logger { get; set; }
        private Stopwatch Stopwatch { get; set; }
        public TestContext TestContext { get; set; }

        [TestInitialize]
        public void TestInitialize()
        {
            Stopwatch = Stopwatch.StartNew();
            Logger = new MsTestLogger(TestContext);
            Logger.Trace($"Starting test - {TestContext.TestName}", SeverityLevel.Information);
            Logger.Event(TestStartedEventName);

            _listener = new ConsoleEventListener(s_eventProviders);
        }

        [TestCleanup]
        public async Task TestCleanupAsync()
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
            await Logger.SafeFlushAsync().ConfigureAwait(false);

            // Dispose the managed resources, so that each test run starts with a fresh slate.
            Dispose();
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
