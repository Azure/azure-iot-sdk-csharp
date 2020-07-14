// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.E2ETests.Helpers.HostNameHelper;

namespace Microsoft.Azure.Devices.E2ETests
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
        /// <param name="namePrefix"></param>
        /// <param name="type"></param>
        public static async Task<TestModule> GetTestModuleAsync(string deviceNamePrefix, string moduleNamePrefix)
        {
            var log = TestLogging.GetInstance();
            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(deviceNamePrefix).ConfigureAwait(false);

            string deviceName = testDevice.Id;
            string moduleName = "E2E_" + moduleNamePrefix + Guid.NewGuid();

            using var rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            log.WriteLine($"{nameof(GetTestModuleAsync)}: Creating module for device {deviceName}.");

            var requestModule = new Module(deviceName, moduleName);
            Module module = await rm.AddModuleAsync(requestModule).ConfigureAwait(false);

            await rm.CloseAsync().ConfigureAwait(false);

            var ret = new TestModule(module);

            log.WriteLine($"{nameof(GetTestModuleAsync)}: Using device {ret.DeviceId} with module {ret.Id}.");
            return ret;
        }

        /// <summary>
        /// Used in conjunction with ModuleClient.CreateFromConnectionString()
        /// </summary>
        public string ConnectionString
        {
            get
            {
                string iotHubHostName = GetHostName(Configuration.IoTHub.ConnectionString);
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
