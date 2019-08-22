// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Model;
using Newtonsoft.Json;

namespace EnvironmentalSensorSample
{
    public class EnvironmentalSensorInterface : DigitalTwinInterface
    {
        public EnvironmentalSensorInterface(string interfaceId, string interfaceName) :
            base(interfaceId, interfaceName, new Callbacks(EnvironmentalSensorPropertiesCallbackAsync, EnvironmentalSensorCommandsCallbackAsync)){ }
        

        #region Read-Only properties
        public async Task DeviceStatePropertyAsync(DeviceStateEnum state)
        {
            var propertyCollection = new Dictionary<string, string>();
            propertyCollection.Add(Constants.DeviceState, state.ToString());
            string output = JsonConvert.SerializeObject(propertyCollection);
            await ReportPropertiesAsync(new Memory<byte>(Encoding.UTF8.GetBytes(output))).ConfigureAwait(false);
        }
        #endregion

        #region Read-write properties
        public static async Task EnvironmentalSensorPropertiesCallbackAsync(DigitalTwinPropertyUpdate propertyUpdate, object userContext)
        {
            Console.WriteLine($"Received updates for property {propertyUpdate.PropertyName} = {Encoding.UTF8.GetString(propertyUpdate.PropertyDesired.Span)} with version = {propertyUpdate.DesiredVersion}");

            //TODO: dispatch property update
        }
        #endregion

        #region Telemetry
        public async Task SendTemperatureAsync(double temperature)
        {
            await SendTelemetryAsync(Constants.Temperature, Encoding.UTF8.GetBytes(temperature.ToString())).ConfigureAwait(false);
        }

        public async Task SendHumidityAsync(double humidity)
        {
            await SendTelemetryAsync(Constants.Humidity, Encoding.UTF8.GetBytes(humidity.ToString())).ConfigureAwait(false);
        }
        #endregion

        #region Commands
        public static Task<DigitalTwinCommandResponse> EnvironmentalSensorCommandsCallbackAsync(DigitalTwinCommandRequest commandRequest, object userContext)
        {
            Console.WriteLine($"\t Command - {commandRequest.Name} was invoked from the service");
            Console.WriteLine($"\t Data - {Encoding.UTF8.GetString(commandRequest.Payload.Span)}");
            Console.WriteLine($"\t Request Id - {commandRequest.RequestId}.");

            // TODO: trigger the callback and return command response
            return Task.FromResult(new DigitalTwinCommandResponse(200, Encoding.UTF8.GetBytes("{\"payload\": \"data\"}")));
        }
        #endregion
    }
}
