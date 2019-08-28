// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json;

namespace EnvironmentalSensorSample
{
    public class EnvironmentalSensorInterface : DigitalTwinInterfaceClient
    {
        private static string environmentalSensorInterfaceId = "urn:csharp_sdk_sample:EnvironmentalSensor:1";

        public EnvironmentalSensorInterface(string interfaceName) :
            base(environmentalSensorInterfaceId, interfaceName, true, true) {
        }

        #region Read-Only properties
        public async Task DeviceStatePropertyAsync(bool state)
        {
            var deviceStateProperty = new DigitalTwinPropertyReport(Constants.DeviceState, state.ToString().ToLower());
            await this.ReportPropertiesAsync(new Collection<DigitalTwinPropertyReport> { deviceStateProperty }).ConfigureAwait(false);
        }
        #endregion

        #region Read-write properties
        public override async Task OnPropertyUpdated(DigitalTwinPropertyUpdate propertyUpdate)
        {
            Console.WriteLine($"Received updates for property {propertyUpdate.PropertyName} = {propertyUpdate.PropertyDesired} with version = {propertyUpdate.DesiredVersion}");

            switch (propertyUpdate.PropertyName)
            {
                case Constants.CustomerName:
                    await this.SetCustomerNameAsync(propertyUpdate).ConfigureAwait(false);
                    break;
                default:
                    Console.WriteLine($"Property name '{propertyUpdate.PropertyName}' is not handled.");
                    break;
            }
        }

        public async Task SetCustomerNameAsync(DigitalTwinPropertyUpdate customerNameUpdate)
        {
            // code to consume customer value, currently just displaying on screen.
            string customerName = customerNameUpdate.PropertyDesired;
            Console.WriteLine($"Desired customer name is '{customerName}'.");

            // report Completed
            //await ReportReadWritePropertyStatusAsync(Constants.CustomerName, new PnPPropertyResponse(customerNameUpdatedValue, desiredVersion, PnPPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
            Console.WriteLine("Sent completed status.");
        }

        public async Task SetBrightnessAsync(DigitalTwinPropertyUpdate brightnessUpdate)
        {
            // code to consume light brightness value, currently just displaying on screen
            string brightness = brightnessUpdate.PropertyDesired;
            Console.WriteLine($"Desired brightness value is {brightness}.");

            // report Pending
            //await ReportReadWritePropertyStatusAsync(Constants.Brightness, new PnPPropertyResponse(brightnessUpdate, desiredVersion, PnPPropertyStatusCode.Pending, "Processing Request")).ConfigureAwait(false);
            Console.WriteLine("Sent pending status for brightness property.");

            // Pretend calling command to Sensor to update brightness
            await Task.Delay(5 * 1000).ConfigureAwait(false);

            // report Completed
            //await ReportReadWritePropertyStatusAsync(Constants.Brightness, new PnPPropertyResponse(brightnessUpdate, desiredVersion, PnPPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
            Console.WriteLine("Sent completed status for brightness property.");
        }
        #endregion

        #region Telemetry
        public async Task SendTemperatureAsync(double temperature)
        {
            await this.SendTelemetryAsync(Constants.Temperature, temperature.ToString()).ConfigureAwait(false);
        }

        public async Task SendHumidityAsync(double humidity)
        {
            await this.SendTelemetryAsync(Constants.Humidity, humidity.ToString()).ConfigureAwait(false);
        }
        #endregion

        #region Commands

        public override Task<DigitalTwinCommandResponse> OnCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            Console.WriteLine($"\t Command - {commandRequest.Name} was invoked from the service");
            Console.WriteLine($"\t Data - {commandRequest.Payload}");
            Console.WriteLine($"\t Request Id - {commandRequest.RequestId}.");

            // TODO: trigger the callback and return command response
            return Task.FromResult(new DigitalTwinCommandResponse(200, "{\"payload\": \"data\"}"));
        }
        #endregion
    }
}
