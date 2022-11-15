// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Tracing;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

// Workers = 0 makes the test engine use one worker per available core. It does not mean to run serially.
[assembly: Parallelize(Workers = 0, Scope = ExecutionScope.ClassLevel)]

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class creates an instance of the logger for each test and logs the test result along with other useful information.
    /// </summary>
    public class E2EMsTestBase : IDisposable
    {
        private ConsoleEventListener _listener;

        // Test specific logger instance
        public TestContext TestContext { get; set; }

        // The test timeout for typical e2e tests
        protected const int TestTimeoutMilliseconds = 3 * 60 * 1000; // 3 minutes

        // The test timeout for long running e2e tests
        protected const int LongRunningTestTimeoutMilliseconds = 5 * 60 * 1000; // 5 minutes

        // The test timeout for long running e2e tests the inspect the connection status change logic on disabling a device.
        protected const int ConnectionStateChangeTestTimeoutMilliseconds = 10 * 60 * 1000; // 10 minutes

        // The test timeout for e2e tests that involve testing token refresh
        protected const int TokenRefreshTestTimeoutMilliseconds = 20 * 60 * 1000; // 20 minutes

        private const string CollectSdkLogsEnvVar = "COLLECT_SDK_LOGS";
        public static readonly bool s_collectSdkLogs;

        static E2EMsTestBase()
        {
            if (bool.TryParse(Environment.GetEnvironmentVariable(CollectSdkLogsEnvVar), out bool collectSdkLogs))
            {
                s_collectSdkLogs = collectSdkLogs;
            }
        }

        [TestInitialize]
        public void TestInitialize()
        {
            VerboseTestLogger.WriteLine($"SDK logs collection is '{s_collectSdkLogs}' based on environment variable '{CollectSdkLogsEnvVar}'.");
            if (s_collectSdkLogs)
            {
                _listener = new ConsoleEventListener();
            }
        }

        [TestCleanup]
        public void TestCleanup()
        {
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
                _listener?.Dispose();
            }
        }

        protected static async Task CleanupAsync(IotHubServiceClient serviceClient, string deviceId)
        {
            if (deviceId == null)
            {
                return;
            }

            // cleanup
            try
            {
                await serviceClient.Devices.DeleteAsync(deviceId).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Test clean up of device {deviceId} failed due to {ex}.");

            }
        }
    }
}
