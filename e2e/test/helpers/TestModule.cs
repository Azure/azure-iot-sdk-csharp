// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class TestModule
    {
        private readonly Module _module;

        private TestModule(Module module)
        {
            _module = module;
        }

        /// <summary>
        /// Factory method.
        /// </summary>
        public static async Task<TestModule> GetTestModuleAsync(string deviceNamePrefix, string moduleNamePrefix)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(deviceNamePrefix).ConfigureAwait(false);

            string deviceName = testDevice.Id;
            string moduleName = "E2E_" + moduleNamePrefix + Guid.NewGuid();

            using var rm = RegistryManager.CreateFromConnectionString(TestConfiguration.IotHub.ConnectionString);
            VerboseTestLogger.WriteLine($"{nameof(GetTestModuleAsync)}: Creating module for device {deviceName}.");

            var requestModule = new Module(deviceName, moduleName);
            Module module = await rm.AddModuleAsync(requestModule).ConfigureAwait(false);

            await rm.CloseAsync().ConfigureAwait(false);

            var ret = new TestModule(module);

            VerboseTestLogger.WriteLine($"{nameof(GetTestModuleAsync)}: Using device {ret.DeviceId} with module {ret.Id}.");
            return ret;
        }

        /// <summary>
        /// Used in conjunction with ModuleClient.CreateFromConnectionString()
        /// </summary>
        public string ConnectionString
        {
            get
            {
                string iotHubHostName = GetHostName(TestConfiguration.IotHub.ConnectionString);
                return $"HostName={iotHubHostName};DeviceId={_module.DeviceId};ModuleId={_module.Id};SharedAccessKey={_module.Authentication.SymmetricKey.PrimaryKey}";
            }
        }

        /// <summary>
        /// Module ID
        /// </summary>
        public string Id => _module.Id;

        /// <summary>
        /// Device ID
        /// </summary>
        public string DeviceId => _module.DeviceId;
    }
}
