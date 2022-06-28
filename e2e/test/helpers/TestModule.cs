// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Devices;
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
        /// <param name="namePrefix"></param>
        /// <param name="type"></param>
        public static async Task<TestModule> GetTestModuleAsync(string deviceNamePrefix, string moduleNamePrefix, MsTestLogger logger)
        {
            using TestDevice testDevice = await TestDevice.GetTestDeviceAsync(logger, deviceNamePrefix).ConfigureAwait(false);

            string deviceName = testDevice.Id;
            string moduleName = "E2E_" + moduleNamePrefix + Guid.NewGuid();

            using var rc = new RegistryClient(TestConfiguration.IoTHub.ConnectionString);
            logger.Trace($"{nameof(GetTestModuleAsync)}: Creating module for device {deviceName}.");

            var requestModule = new Module(deviceName, moduleName);
            Module module = await rc.AddModuleAsync(requestModule).ConfigureAwait(false);

            var ret = new TestModule(module);

            logger.Trace($"{nameof(GetTestModuleAsync)}: Using device {ret.DeviceId} with module {ret.Id}.");
            return ret;
        }

        /// <summary>
        /// Used in conjunction with ModuleClient.CreateFromConnectionString()
        /// </summary>
        public string ConnectionString
        {
            get
            {
                string iotHubHostName = GetHostName(TestConfiguration.IoTHub.ConnectionString);
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
