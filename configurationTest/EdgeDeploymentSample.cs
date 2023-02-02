// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Samples
{
    public class EdgeDeploymentSample
    {
        private const string DeviceIdPrefix = "EdgeDeploymentSample_";
        private const string ConfigurationIdPrefix = "edgedeploymentsampleconfiguration-";
        private const int NumOfEdgeDevices = 10;
        private const int BasePriority = 2;
        private readonly RegistryManager _registryManager;

        private readonly List<Configuration> _configurationsToDelete = new List<Configuration>();

        public EdgeDeploymentSample(RegistryManager registryManager)
        {
            _registryManager = registryManager ?? throw new ArgumentNullException(nameof(registryManager));
        }

        public async Task RunSampleAsync()
        {
            try
            {
                IEnumerable<Configuration> configurations = await _registryManager.GetConfigurationsAsync(100);
                Console.WriteLine(configurations.Count());
                foreach (Configuration configuration in configurations)
                {
                    string configurationId = configuration.Id;
                    Console.WriteLine(configurationId);
                    //await _registryManager.RemoveConfigurationAsync(configurationId);
                    //configurations = await _registryManager.GetConfigurationsAsync(100);
                    //Console.WriteLine(configurations.Count());
                }
                //await _registryManager.RemoveConfigurationAsync("registrymanager_exportdevicesd2e9a7ce-c6bf-4a1e-ae67-656aa33fc524");
                configurations = await _registryManager.GetConfigurationsAsync(100);
                Configuration config = await _registryManager.GetConfigurationAsync("registrymanager_exportdevicesd2e9a7ce-c6bf-4a1e-ae67-656aa33fc524");
                Console.WriteLine(config);

                Console.WriteLine(configurations.Count());
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private async Task CleanUpConfigurationsAsync()
        {
        }
    }
}
