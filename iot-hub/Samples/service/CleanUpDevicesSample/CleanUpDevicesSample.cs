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
                "xdevice1",
                "doNotDelete1"
            };

        public CleanUpDevicesSample(RegistryManager rm)
        {
            _rm = rm ?? throw new ArgumentNullException(nameof(rm));
        }

        public async Task RunSampleAsync()
        {
            try
            {
                int devicesDeleted = 0;
                Console.WriteLine("Get devices");
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
