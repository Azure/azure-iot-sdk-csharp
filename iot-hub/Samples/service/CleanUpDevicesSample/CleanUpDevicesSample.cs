using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class CleanUpDevicesSample
    {
        private RegistryManager _rm;
        private List<string> devicesToBeRetained =
            new List<string>{
                "DoNotDelete1"
            };

        public CleanUpDevicesSample(RegistryManager rm)
        {
            _rm = rm ?? throw new ArgumentNullException(nameof(rm));
        }

        public async Task RunSampleAsync()
        {
            try
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

                int devicesDeleted = 0;
                Console.WriteLine("Clean up devices:");
                string sqlQueryString = "select * from devices";
                IQuery query = _rm.CreateQuery(sqlQueryString);
                while (query.HasMoreResults)
                {
                    IEnumerable<Shared.Twin> result = await query.GetNextAsTwinAsync();
                    foreach (var twinResult in result)
                    {
                        string deviceId = twinResult.DeviceId;
                        if (!devicesToBeRetained.Contains(deviceId))
                        {
                            Console.WriteLine($"Removing Device ID: {deviceId}");
                            await _rm.RemoveDeviceAsync(deviceId).ConfigureAwait(false);
                            devicesDeleted++;
                        }
                    }
                }
                Console.Write($"Total no of devices deleted: {devicesDeleted}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception {ex.Message}");
            }
        }
    }
}
