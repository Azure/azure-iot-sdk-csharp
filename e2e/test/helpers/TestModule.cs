// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class TestModule : IAsyncDisposable
    {
        private Module _module;
        private TestDevice _testDevice;

        /// <summary>
        /// Used in conjunction with ModuleClient.CreateFromConnectionString()
        /// </summary>
        public string ConnectionString =>
            $"HostName={TestConfiguration.IotHub.GetIotHubHostName()};DeviceId={_module.DeviceId};ModuleId={_module.Id};SharedAccessKey={_module.Authentication.SymmetricKey.PrimaryKey}";

        /// <summary>
        /// Module Id
        /// </summary>
        public string Id => _module.Id;

        /// <summary>
        /// Device Id
        /// </summary>
        public string DeviceId => _testDevice.Id;

        /// <summary>
        /// Factory method.
        /// </summary>
        public static async Task<TestModule> GetTestModuleAsync(string deviceNamePrefix, string moduleNamePrefix)
        {
            var testModule = new TestModule
            {
                _testDevice = await TestDevice.GetTestDeviceAsync(deviceNamePrefix).ConfigureAwait(false)
            };

            string deviceName = testModule._testDevice.Id;
            string moduleName = $"E2E_{moduleNamePrefix}_{Guid.NewGuid()}";

            IotHubServiceClient sc = TestDevice.ServiceClient;
            testModule._module = await sc.Modules.CreateAsync(new Module(deviceName, moduleName)).ConfigureAwait(false);
            VerboseTestLogger.WriteLine($"{nameof(GetTestModuleAsync)}: Using device {testModule.DeviceId} with module {testModule.Id}.");

            return testModule;
        }

        public async ValueTask DisposeAsync()
        {
            await _testDevice.DisposeAsync().ConfigureAwait(false);
            GC.SuppressFinalize(this);
        }
    }
}
