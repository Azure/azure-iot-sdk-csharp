// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;
using Azure.Iot.DigitalTwin.Device;

namespace EnvironmentalSensorSample
{
    public class DigitalTwinClientSample
    {
        // Interfaces implemented by the device
        private static string environmentalSensorInterfaceId = "urn:contoso:environmentalsensor:1";
        private static string environmentalSensorInterfaceName = "environmentalsensor";

        private static string capabilityModelId = "urn:contoso:com:dcm:1";

        private static EnvironmentalSensorInterface environmentalSensorInterface;
        private static DeviceInformationInterface deviceInformationInterface;
        private DigitalTwinClient digitalTwinClient;

        public DigitalTwinClientSample(DigitalTwinClient digitalTwinClient)
        {
            this.digitalTwinClient = digitalTwinClient;

            // create environmental sensor interface
            environmentalSensorInterface = new EnvironmentalSensorInterface(environmentalSensorInterfaceId, environmentalSensorInterfaceName);

            // create device information interface
            deviceInformationInterface = new DeviceInformationInterface();
        }

        public async Task RunSampleAsync()
        {
            // register interface(s) for the device
            await digitalTwinClient.RegisterInterfacesAsync(capabilityModelId, new DigitalTwinInterface[] { deviceInformationInterface, environmentalSensorInterface }).ConfigureAwait(false);

            // send device information
            deviceInformationInterface.SetFirmwareVersion("456888");
            deviceInformationInterface.SetHardwareVersion("9000C1");
            deviceInformationInterface.SetManufacturer("element14");
            deviceInformationInterface.SetModel("ModelIDxcdvmk");
            deviceInformationInterface.SetOriginalEquipmentManufacturer("Raspberry Pi");
            deviceInformationInterface.SetOperatingSystemName("Windows 10");
            deviceInformationInterface.SetOperatingSystemVersion("122.0.0");
            deviceInformationInterface.SetProcessorArchitecture("64-bit");
            deviceInformationInterface.SetProcessorType("ARMv8 CPU");
            deviceInformationInterface.SetSerialNumber("JH786AB0");
            deviceInformationInterface.SetTotalMemory(1024);
            deviceInformationInterface.SetTotalStorage(256);
            await deviceInformationInterface.SendAllPropertiesAsync().ConfigureAwait(false);

            // report properties data
            await environmentalSensorInterface.DeviceStatePropertyAsync(DeviceStateEnum.Online).ConfigureAwait(false);

            // send telemetry
            await environmentalSensorInterface.SendTemperatureAsync(37);
            await environmentalSensorInterface.SendHumidityAsync(28);
        }
    }
}
