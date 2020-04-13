// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class CleanUpDevicesSample
    {
        private const int QueryBatchSize = 10000;
        private const int DeleteBatchSize = 100;
        private readonly RegistryManager _rm;
        private readonly List<string> _deleteDeviceWithPrefix;
        private readonly List<Device> _devicesToDelete = new List<Device>();

        public CleanUpDevicesSample(RegistryManager rm, List<string> deleteDeviceWithPrefix)
        {
            _rm = rm ?? throw new ArgumentNullException(nameof(rm));
            _deleteDeviceWithPrefix = deleteDeviceWithPrefix;
        }

        public async Task RunCleanUpAsync()
        {
            // Clean up devices
            await PrintDeviceCountAsync().ConfigureAwait(false);
            await CreateDevicesListForDeletion().ConfigureAwait(false);
            await CleanUpDevices().ConfigureAwait(false);
        }

        private async Task CleanUpDevices()
        {
            int devicesDeleted = 0;

            Console.WriteLine("Clean up devices:");
            var _bulkDeleteList = new List<Device>(DeleteBatchSize);
            while (_devicesToDelete.Count > 0)
            {
                try
                {
                    int i;
                    for (i = 0; (i < DeleteBatchSize) && (i < _devicesToDelete.Count); i++)
                    {
                        _bulkDeleteList.Add(_devicesToDelete[i]);
                        Console.WriteLine($"\tRemove: {_devicesToDelete[i].Id}");
                    }

                    BulkRegistryOperationResult ret = await _rm.RemoveDevices2Async(_bulkDeleteList, true, CancellationToken.None).ConfigureAwait(false);
                    int successfulDeletionCount = _bulkDeleteList.Count - ret.Errors.Length;
                    devicesDeleted += successfulDeletionCount;
                    Console.WriteLine($"BATCH DELETE: Current batch - {successfulDeletionCount}");
                    Console.WriteLine($"BATCH DELETE: Running total - {devicesDeleted}");
                    if (!ret.IsSuccessful)
                    {
                        foreach (DeviceRegistryOperationError error in ret.Errors)
                        {
                            Console.WriteLine($"\tERROR: {error.DeviceId} - {error.ErrorCode}: {error.ErrorStatus}");
                        }
                    }
                    _devicesToDelete.RemoveRange(0, i);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Exception thrown, continue to next batch: {ex}");
                }
                finally
                {
                    _bulkDeleteList.Clear();
                }
            }

            Console.WriteLine($"-- Total # of devices deleted: {devicesDeleted}");
        }

        private async Task CreateDevicesListForDeletion()
        {
            Console.WriteLine("Query devices for cleanup:");
            string sqlQueryString = "select * from devices";
            IQuery query = _rm.CreateQuery(sqlQueryString, QueryBatchSize);

            while (query.HasMoreResults)
            {
                IEnumerable<Shared.Twin> result = await query.GetNextAsTwinAsync().ConfigureAwait(false);
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
                Console.WriteLine($"Retrieved {_devicesToDelete.Count} devices for deletion...");
            }
        }

        private async Task PrintDeviceCountAsync()
        {
            string countSqlQuery = "SELECT COUNT() AS numberOfDevices FROM devices";
            IQuery countQuery = _rm.CreateQuery(countSqlQuery);
            while (countQuery.HasMoreResults)
            {
                IEnumerable<string> result = await countQuery.GetNextAsJsonAsync().ConfigureAwait(false);
                foreach (var item in result)
                {
                    Console.WriteLine($"Total # of devices in the hub: \n{item}");
                }
            }
        }
    }
}

