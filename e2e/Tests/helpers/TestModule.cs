// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class TestModule : IAsyncDisposable
    {
        /// <summary>
        /// Used in conjunction with ModuleClient.CreateFromConnectionString()
        /// </summary>
        internal string ConnectionString =>
            $"HostName={TestConfiguration.IotHub.GetIotHubHostName()};DeviceId={Module.DeviceId};ModuleId={Module.Id};SharedAccessKey={Module.Authentication.SymmetricKey.PrimaryKey}";

        /// <summary>
        /// Module Id
        /// </summary>
        internal string Id => Module.Id;

        /// <summary>
        /// Device Id
        /// </summary>
        internal string DeviceId => TestDevice.Id;

        /// <summary>
        /// The device that hosts the module.
        /// </summary>
        internal TestDevice TestDevice { get; set; }

        /// <summary>
        /// The created module.
        /// </summary>
        internal Module Module { get; set; }

        /// <summary>
        /// Factory method.
        /// </summary>
        internal static async Task<TestModule> GetTestModuleAsync(string deviceNamePrefix, string moduleNamePrefix)
        {
            var testModule = new TestModule
            {
                TestDevice = await TestDevice.GetTestDeviceAsync(deviceNamePrefix).ConfigureAwait(false)
            };

            string deviceName = testModule.TestDevice.Id;
            string moduleName = $"E2E_{moduleNamePrefix}_{Guid.NewGuid()}";

            IotHubServiceClient sc = TestDevice.ServiceClient;
            testModule.Module = await sc.Modules.CreateAsync(new Module(deviceName, moduleName)).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"{nameof(GetTestModuleAsync)}: Using device {testModule.DeviceId} with module {testModule.Id}.");

            return testModule;
        }

        public async ValueTask DisposeAsync()
        {
            // This will delete the device and the module along with it.
            await TestDevice.DisposeAsync().ConfigureAwait(false);

            GC.SuppressFinalize(this);
        }
    }
}
