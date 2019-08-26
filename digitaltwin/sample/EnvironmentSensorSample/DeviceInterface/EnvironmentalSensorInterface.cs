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
    public class EnvironmentalSensorInterface : DigitalTwinInterfaceClient
    {
        public EnvironmentalSensorInterface(string interfaceId, string interfaceName) : 
            base(interfaceId, interfaceName) {
            //new Callbacks(EnvironmentalSensorPropertiesCallbackAsync, EnvironmentalSensorCommandsCallbackAsync)
        }



        #region Read-Only properties
        public async Task DeviceStatePropertyAsync(bool state)
        {
            var propertyCollection = new Dictionary<string, bool>();
            propertyCollection.Add(Constants.DeviceState, state);
            string output = JsonConvert.SerializeObject(propertyCollection);
            await ReportPropertiesAsync(new Memory<byte>(Encoding.UTF8.GetBytes(output))).ConfigureAwait(false);
        }
        #endregion
        
        #region Read-write properties
        public Task<DigitalTwinCommandResponse> EnvironmentalSensorPropertiesCallbackAsync(DigitalTwinCommandRequest commandRequest, object userContext)
        {

            //Console.WriteLine($"Received updates for property {propertyUpdate.PropertyName} = {Encoding.UTF8.GetString(propertyUpdate.PropertyDesired.Span)} with version = {propertyUpdate.DesiredVersion}");

            //if (propertyUpdate.PropertyName.Equals(Constants.CustomerName))
            //{
            //    //await digitalTwinInterface.ReportReadWritePropertyStatusAsync(Constants.CustomerName, new PnPPropertyResponse(customerNameUpdatedValue, desiredVersion, PnPPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
            //    Console.WriteLine("Sent completed status.");
            //}

            //TODO: dispatch property update

            return Task.FromResult<DigitalTwinCommandResponse>(null);
        }

        //public async Task SetCustomerNameAsync(DigitalTwinPropertyUpdate propertyUpdate, object userContext)
        //{
        //    // code to consume customer value, currently just displaying on screen.
        //    Console.WriteLine($"Customer name received is {customerNameUpdatedValue.Value}.");

        //    // report Completed
        //    await ReportReadWritePropertyStatusAsync(Constants.CustomerName, new PnPPropertyResponse(customerNameUpdatedValue, desiredVersion, PnPPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
        //    Console.WriteLine("Sent completed status.");
        //}

        //public async Task SetBrightnessAsync(PnPValue brightnessUpdatedValue, long desiredVersion, object userContext)
        //{
        //    // code to consume light brightness value, currently just displaying on screen
        //    Console.WriteLine($"Updated brightness value is {brightnessUpdatedValue.Value}.");

        //    // report Pending
        //    await ReportReadWritePropertyStatusAsync(Constants.Brightness, new PnPPropertyResponse(brightnessUpdatedValue, desiredVersion, PnPPropertyStatusCode.Pending, "Processing Request")).ConfigureAwait(false);
        //    Console.WriteLine("Sent pending status for brightness property.");

        //    // do some action
        //    await Task.Delay(5 * 1000).ConfigureAwait(false);
        //    Console.WriteLine("Run script to update the time interval of telemetry frequency (in seconds).");

        //    // report Completed
        //    await ReportReadWritePropertyStatusAsync(Constants.Brightness, new PnPPropertyResponse(brightnessUpdatedValue, desiredVersion, PnPPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
        //    Console.WriteLine("Sent completed status for brightness property.");
        //}
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
        public static Task<DigitalTwinCommandResponse> EnvironmentalSensorCommandsCallbackAsync(DigitalTwinInterfaceClient digitalTwinInterface, DigitalTwinCommandRequest commandRequest, object userContext)
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
