// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class CleanUpDevicesSample
    {
        private const int QueryBatchSize = 1000;
        private const int DeleteBatchSize = 100;
        private RegistryManager _rm;
        private List<string> _deleteDeviceWithPrefix =
            new List<string>{
                // C# E2E tests
                "E2E_",

                // C E2E tests
                "e2e_",
                "e2e-",
                "symmetrickey-registration-id-",
                "tpm-registration-id-",
                "csdk_",
                "someregistrationid-",
                "EdgeDeploymentSample_",
            };
        
        private List<string> _deleteConfigurationWithPrefix =
            new List<string>{
                // C# E2E tests
                "edgedeploymentsampleconfiguration-",
            };

        private List<Device> _devicesToDelete = new List<Device>();
        private List<Configuration> _configurationsToDelete = new List<Configuration>();


        public CleanUpDevicesSample(RegistryManager rm)
        {
            _rm = rm ?? throw new ArgumentNullException(nameof(rm));
        }

        public async Task RunSampleAsync()
        {
            try
            {
                await PrintDeviceCount();

                int devicesDeleted = 0;
                Console.WriteLine("Clean up devices:");
                string sqlQueryString = "select * from devices";
                IQuery query = _rm.CreateQuery(sqlQueryString, QueryBatchSize);

                while (query.HasMoreResults)
                {
                    IEnumerable<Shared.Twin> result = await query.GetNextAsTwinAsync();
                    foreach (var twinResult in result)
                    {
                        string deviceId = twinResult.DeviceId;
                        foreach (string prefix in _deleteDeviceWithPrefix)
                        {
                            if (deviceId.StartsWith(prefix))
                            {
                                _devicesToDelete.Add(new Device(deviceId));
                            }
                        }
                    }
                }

                var _bulkDeleteList = new List<Device>(DeleteBatchSize);
                while (_devicesToDelete.Count > 0)
                {
                    int i;
                    for (i = 0; (i < DeleteBatchSize) && (i < _devicesToDelete.Count); i++)
                    {
                        _bulkDeleteList.Add(_devicesToDelete[i]);
                        Console.WriteLine($"\tRemove: {_devicesToDelete[i].Id}");
                    }

                    _devicesToDelete.RemoveRange(0, i);

                    BulkRegistryOperationResult ret = await _rm.RemoveDevices2Async(_bulkDeleteList, true, CancellationToken.None).ConfigureAwait(false);
                    devicesDeleted += _bulkDeleteList.Count - ret.Errors.Length;
                    Console.WriteLine($"BATCH DELETE: {devicesDeleted}/{_bulkDeleteList.Count}");
                    if (!ret.IsSuccessful)
                    {
                        foreach (DeviceRegistryOperationError error in ret.Errors)
                        {
                            Console.WriteLine($"\tERROR: {error.DeviceId} - {error.ErrorCode}: {error.ErrorStatus}");
                        }
                    }

                    _bulkDeleteList.Clear();
                }

                Console.WriteLine($"-- Total no of devices deleted: {devicesDeleted}");
            
                var configurations = await _rm.GetConfigurationsAsync(100, new CancellationToken()).ConfigureAwait(false);
                {
                    foreach (var configuration in configurations)
                    {
                        string configurationId = configuration.Id;
                        foreach (var prefix in _deleteConfigurationWithPrefix)
                        {
                            if (configurationId.StartsWith(prefix))
                            {
                            _configurationsToDelete.Add(new Configuration(configurationId));
                            }
                        }
                    }
                }
                
                var removeConfigTasks = new List<Task>();
                _configurationsToDelete.ForEach(configuration =>
                {
                    Console.WriteLine($"Remove: {configuration.Id}");
                    removeConfigTasks.Add(_rm.RemoveConfigurationAsync(configuration.Id));
                });

                Task.WaitAll(removeConfigTasks.ToArray());
                Console.WriteLine($"-- Total no of configurations deleted: {_configurationsToDelete.Count}");
            
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private async Task PrintDeviceCount()
        {
            string countSqlQuery = "SELECT COUNT() AS numberOfDevices FROM devices";
            IQuery countQuery = _rm.CreateQuery(countSqlQuery);
            while (countQuery.HasMoreResults)
            {
                IEnumerable<string> result = await countQuery.GetNextAsJsonAsync().ConfigureAwait(false);
                foreach (var item in result)
                {
                    Console.WriteLine($"Total no of devices on the hub: \n{item}");
                }
            }
        }
    }
}

