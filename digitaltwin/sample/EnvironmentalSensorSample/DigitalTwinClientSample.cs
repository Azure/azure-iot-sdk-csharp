// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device;

namespace EnvironmentalSensorSample
{
    public class DigitalTwinClientSample
    {
        private static string environmentalSensorInterfaceName = "environmentalSensor";
        private static string capabilityModelId = "urn:csharp_sdk_sample:sample_device:1";

        private static EnvironmentalSensorInterface environmentalSensorInterface;
        private static DeviceInformationInterface deviceInformationInterface;
        private DigitalTwinClient digitalTwinClient;

        public DigitalTwinClientSample(DigitalTwinClient digitalTwinClient)
        {
            this.digitalTwinClient = digitalTwinClient;

            // create environmental sensor interface
            environmentalSensorInterface = new EnvironmentalSensorInterface(environmentalSensorInterfaceName);

            // create device information interface
            deviceInformationInterface = new DeviceInformationInterface();
        }

        public async Task RunSampleAsync()
        {
            // register interface(s) for the device
            await this.digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterfaceClient[] { deviceInformationInterface, environmentalSensorInterface }).ConfigureAwait(false);

            // send device information
            deviceInformationInterface.SetManufacturer("element15");
            deviceInformationInterface.SetModel("ModelIDxcdvmk");
            deviceInformationInterface.SetSoftwareVersion("1.0.0");
            deviceInformationInterface.SetOperatingSystemName("Windows 10");
            deviceInformationInterface.SetProcessorArchitecture("64-bit");
            deviceInformationInterface.SetProcessorManufacturer("Intel");
            deviceInformationInterface.SetTotalMemory(1024);
            deviceInformationInterface.SetTotalStorage(256);
            await deviceInformationInterface.SendAllPropertiesAsync().ConfigureAwait(false);

            // report properties data
            await environmentalSensorInterface.DeviceStatePropertyAsync(true).ConfigureAwait(false);

            // send telemetry
            await environmentalSensorInterface.SendTemperatureAsync(37);
            await environmentalSensorInterface.SendHumidityAsync(28);
        }
    }
}
