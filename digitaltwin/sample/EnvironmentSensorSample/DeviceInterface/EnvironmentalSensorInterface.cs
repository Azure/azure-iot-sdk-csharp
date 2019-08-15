// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.DigitalTwin.Client;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;

namespace EnvironmentalSensorSample
{
    public class EnvironmentalSensorInterface : DigitalTwinInterface
    {
        public EnvironmentalSensorInterface(string interfaceId, string interfaceName) : base(interfaceId, interfaceName) { }

        #region Read-Only properties
        public async Task DeviceStatePropertyAsync(DeviceStateEnum state)
        {
            List<DigitalTwinProperty> propertyCollection = new List<DigitalTwinProperty>();
            propertyCollection.Add(new DigitalTwinProperty(Constants.DeviceState, DigitalTwinValue.CreateString(state.ToString())));
            await ReportPropertiesAsync(propertyCollection).ConfigureAwait(false);
        }
        #endregion

        #region Read-write properties
        public async Task SetCustomerNameAsync(DigitalTwinValue customerNameUpdatedValue, long desiredVersion, object userContext)
        {
            // code to consume customer value, currently just displaying on screen.
            Console.WriteLine($"Customer name received is {customerNameUpdatedValue.Value}.");

            // report Completed
            //await ReportPropertyStatusAsync(Constants.CustomerName, new DigitalTwinPropertyResponse(customerNameUpdatedValue, desiredVersion, DigitalTwinPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
            Console.WriteLine("Sent completed status.");
        }

        public async Task SetBrightnessAsync(DigitalTwinValue brightnessUpdatedValue, long desiredVersion, object userContext)
        {
            // code to consume light brightness value, currently just displaying on screen
            Console.WriteLine($"Updated brightness value is {brightnessUpdatedValue.Value}.");

            // report Pending
            //await ReportPropertyStatusAsync(Constants.Brightness, new DigitalTwinPropertyResponse(brightnessUpdatedValue, desiredVersion, DigitalTwinPropertyStatusCode.Pending, "Processing Request")).ConfigureAwait(false);
            Console.WriteLine("Sent pending status for brightness property.");

            // do some action
            await Task.Delay(5 * 1000).ConfigureAwait(false);
            Console.WriteLine("Run script to update the time interval of telemetry frequency (in seconds).");

            // report Completed
            //await ReportReadWritePropertyStatusAsync(Constants.Brightness, new DigitalTwinPropertyResponse(brightnessUpdatedValue, desiredVersion, DigitalTwinPropertyStatusCode.Completed, "Request completed")).ConfigureAwait(false);
            Console.WriteLine("Sent completed status for brightness property.");
        }
        #endregion

        #region Telemetry
        public async Task SendTemperatureAsync(double temperature)
        {
            await SendTelemetryAsync(new DigitalTwinProperty(Constants.Temperature, DigitalTwinValue.CreateDouble(temperature))).ConfigureAwait(false);
        }

        public async Task SendHumidityAsync(double humidity)
        {
            await SendTelemetryAsync(new DigitalTwinProperty(Constants.Humidity, DigitalTwinValue.CreateDouble(humidity))).ConfigureAwait(false);
        }
        #endregion

        #region Commands
        public Task<DigitalTwinCommandResponse> BlinkCommandAsync(DigitalTwinCommandRequest commandRequest, object userContext)
        {
            Console.WriteLine($"\t {Constants.BlinkCommandName} command was invoked from the service.");

            long timeInterval = (long)commandRequest.RequestSchemaData;
            Console.WriteLine($"Time interval received {timeInterval} milliseconds");

            Console.WriteLine($"Send {Constants.BlinkCommandName} command status: Completed.");
            return Task.FromResult(new DigitalTwinCommandResponse(200));
        }

        public Task<DigitalTwinCommandResponse> TurnOnLightCommandAsync(DigitalTwinCommandRequest commandRequest, object userContext)
        {
            Console.WriteLine($"\t {Constants.TurnOnLightCommad} command was invoked from the service.");

            Console.WriteLine($"Send {Constants.TurnOnLightCommad} command status: Completed.");
            return Task.FromResult(new DigitalTwinCommandResponse(200, DigitalTwinValue.CreateString("Light turned on.")));
        }

        public Task<DigitalTwinCommandResponse> TurnOffLightCommandAsync(DigitalTwinCommandRequest commandRequest, object userContext)
        {
            Console.WriteLine($"\t {Constants.TurnOffLightCommand} command was invoked from the service.");

            Console.WriteLine($"Send {Constants.TurnOffLightCommand} command status: Completed.");
            return Task.FromResult(new DigitalTwinCommandResponse(200, DigitalTwinValue.CreateString("Light turned off.")));
        }
        #endregion

        #region setup
        public async void SetUpCallbacks()
        {
            // register read-write properties
            await SetPropertyUpdatedCallbackAsync(Constants.CustomerName, SetCustomerNameAsync, null).ConfigureAwait(false);
            await SetPropertyUpdatedCallbackAsync(Constants.Brightness, SetBrightnessAsync, null).ConfigureAwait(false);

            // register commands
            //await SetCommandInvokeCallbackAsync(Constants.BlinkCommandName, BlinkCommandAsync, null).ConfigureAwait(false);
            //await SetCommandInvokeCallbackAsync(Constants.TurnOnLightCommad, TurnOnLightCommandAsync, null).ConfigureAwait(false);
            //await SetCommandInvokeCallbackAsync(Constants.TurnOffLightCommand, TurnOffLightCommandAsync, null).ConfigureAwait(false);
        }
        #endregion
    }
}
