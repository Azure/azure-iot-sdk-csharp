// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestModule
    {
        private Module _module;

        private TestModule(Module module)
        {
            _module = module;
        }

        /// <summary>
        /// Factory method.
        /// IMPORTANT: Not thread safe!
        /// </summary>
        /// <param name="namePrefix"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static async Task<TestModule> GetTestModuleAsync(string deviceNamePrefix, string moduleNamePrefix)
        {
            var log = TestLogging.GetInstance();
            string prefix = deviceNamePrefix + "Module" + "_";

            TestDevice testDevice = await TestDevice.GetTestDeviceAsync(prefix).ConfigureAwait(false);

            string deviceName = testDevice.Id;
            string moduleName = moduleNamePrefix + Guid.NewGuid();

            RegistryManager rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            log.WriteLine($"{nameof(GetTestModuleAsync)}: Creating module for device {deviceName}.");

            Module requestModule = new Module(deviceName, moduleName);
            Module module = await rm.AddModuleAsync(requestModule).ConfigureAwait(false);

            await rm.CloseAsync().ConfigureAwait(false);

            TestModule ret = new TestModule(module);

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
        public string Id
        {
            get
            {
                return _module.Id;
            }
        }

        /// <summary>
        /// Device ID
        /// </summary>
        public string DeviceId
        {
            get
            {
                return _module.DeviceId;
            }
        }

        private static string GetHostName(string iotHubConnectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(iotHubConnectionString).Groups[1].Value;
        }

    }
}
