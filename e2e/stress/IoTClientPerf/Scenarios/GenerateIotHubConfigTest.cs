// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public class GenerateIotHubConfigTest : PerfScenario
    {
        // Pattern: <NamePrefix>_<auth>_id
        // - SAS key devices generated will have the same key as the device specified in the IOTHUB_DEVICE_CONN_STRING/IOTHUB_DEVICE_CONN_STRING2.
        // - X509 key devices generated will have the same certificate as specified in IOTHUB_X509_PFX_CERTIFICATE
        private static StreamWriter s_outputFile = new StreamWriter("devices.txt");
        private static SemaphoreSlim s_semaphore = new SemaphoreSlim(1);

        static GenerateIotHubConfigTest()
        {
            Console.WriteLine("----------------------");
            Console.WriteLine("To import the generated file:");
            Console.WriteLine("1. Create an Azure Storage Container");
            Console.WriteLine("2. Manually upload the `devices.txt` file to the BLOB. (One way is to use the Azure Portal.)");
            Console.WriteLine("3. Get a container SAS key:");
            Console.WriteLine($"\taz storage container generate-sas -n <BLOB_NAME> --account-name <STORAGE_ACCOUNT> --account-key <STORAGE_ACCOUNT_ACCESS_KEY> --permissions dlrw --expiry {(DateTime.Now.AddDays(1)).ToString("yyyy-MM-dd")} ");
            Console.WriteLine("\tAlternatively use Microsoft Azure Storage Explorer to generate a full-access (IoT Hub needs both read and write), short lived SAS token URI.");
            Console.WriteLine("4. Import into IoT Hub:");
            Console.WriteLine("\tSet environment variable IOTHUB_IMPORTEXPORT_BLOB_URI=<SAS_URI>");
            Console.WriteLine("\tIoTClientPerf -f import_iothub_config");
        }

        public GenerateIotHubConfigTest(PerfScenarioConfig config) : base(config)
        {
        }

        public override async Task SetupAsync(CancellationToken ct)
        {
            /*
             * {"id":"SASDeviceId","eTag":"MjM0NDA0NDA0","status":"enabled","authentication":{"symmetricKey":{"primaryKey":"<base64Key>","secondaryKey":"<base64Key>"},"x509Thumbprint":{"primaryThumbprint":null,"secondaryThumbprint":null},"type":"sas"},"twinETag":"AAAAAAAAAAE=","tags":{},"properties":{"desired":{},"reported":{}},"capabilities":{"iotEdge":false}}
             * {"id":"X509DeviceId","eTag":"MzUxODMyMDQ0","status":"enabled","authentication":{"symmetricKey":{"primaryKey":null,"secondaryKey":null},"x509Thumbprint":{"primaryThumbprint":"<THUMB>","secondaryThumbprint":null},"type":"selfSigned"},"twinETag":"AAAAAAAAAAE=","tags":{},"properties":{"desired":{},"reported":{}},"capabilities":{"iotEdge":false}}
             */

            string deviceExport = null;

            if (_authType == "sas")
            {
                deviceExport = $"{{\"id\":\"{Configuration.Stress.GetDeviceNameById(_id, _authType)}\",\"eTag\":\"MjM0NDA0NDA0\",\"status\":\"enabled\",\"authentication\":" +
                    $"{{\"symmetricKey\":{{\"primaryKey\":\"{Configuration.Stress.Key1}\",\"secondaryKey\":\"{Configuration.Stress.Key2}\"}}," + 
                    $"\"x509Thumbprint\":{{\"primaryThumbprint\":null,\"secondaryThumbprint\":null}},\"type\":\"sas\"}}," + 
                    $"\"twinETag\":\"AAAAAAAAAAE=\",\"tags\":{{}},\"properties\":{{\"desired\":{{}},\"reported\":{{}}}},\"capabilities\":{{\"iotEdge\":false}}}}";
            }
            else if (_authType == "x509")
            {
                deviceExport = $"{{\"id\":\"{Configuration.Stress.GetDeviceNameById(_id, _authType)}\",\"eTag\":\"MzUxODMyMDQ0\",\"status\":\"enabled\",\"authentication\":" + 
                    $"{{\"symmetricKey\":{{\"primaryKey\":null,\"secondaryKey\":null}}," + 
                    $"\"x509Thumbprint\":{{\"primaryThumbprint\":\"{Configuration.Stress.Certificate.Thumbprint}\",\"secondaryThumbprint\":null}},\"type\":\"selfSigned\"}}," + 
                    $"\"twinETag\":\"AAAAAAAAAAE=\",\"tags\":{{}},\"properties\":{{\"desired\":{{}},\"reported\":{{}}}},\"capabilities\":{{\"iotEdge\":false}}}}";
            }
            else
            {
                throw new NotSupportedException($"Authentication type {_authType} cannot be used to generate devices.");
            }

            await s_semaphore.WaitAsync().ConfigureAwait(false);
            await s_outputFile.WriteLineAsync(deviceExport).ConfigureAwait(false);
            s_semaphore.Release();
        }

        public override async Task TeardownAsync(CancellationToken ct)
        {
            try
            {
                await s_outputFile.FlushAsync().ConfigureAwait(false);
                s_outputFile.Dispose();
            }
            catch (ObjectDisposedException)
            { }
        }

        public override Task RunTestAsync(CancellationToken ct)
        {
            throw new OperationCanceledException();
        }
    }
}
