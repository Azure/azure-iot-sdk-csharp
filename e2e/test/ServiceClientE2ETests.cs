// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class ServiceClientE2ETests : IDisposable
    {
        private readonly string DevicePrefix = $"E2E_{nameof(ServiceClientE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private readonly ConsoleEventListener _listener;

        public ServiceClientE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        [ExpectedException(typeof(TimeoutException))]
        public async Task Message_TimeOutReachedResponse()
        {
            await FastTimeout().ConfigureAwait(false);
        }

        [TestMethod]
        public async Task Message_NoTimeoutPassed()
        {
            await DefaultTimeout().ConfigureAwait(false);
        }

        private async Task FastTimeout()
        {
            TimeSpan? timeout = TimeSpan.FromTicks(5);
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task DefaultTimeout()
        {
            TimeSpan? timeout = null;
            await TestTimeout(timeout).ConfigureAwait(false);
        }

        private async Task TestTimeout(TimeSpan? timeout)
        {
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(DevicePrefix).ConfigureAwait(false);
            using (ServiceClient sender = ServiceClient.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                _log.WriteLine($"Testing ServiceClient SendAsync() timeout in ticks={timeout?.Ticks}");
                try
                {
                    await sender.SendAsync(testDevice.Id, new Message(Encoding.ASCII.GetBytes("Dummy Message")), timeout).ConfigureAwait(false);
                }
                finally
                {
                    _log.WriteLine($"Testing ServiceClient SendAsync(): exiting test after time={sw.Elapsed}; ticks={sw.ElapsedTicks}");
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }
    }
}
