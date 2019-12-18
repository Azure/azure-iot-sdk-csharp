// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

using Microsoft.Azure.IoT.DigitalTwin.Device;
using Microsoft.Azure.IoT.DigitalTwin.Device.Model;

namespace EnvironmentalSensorSample
{
    /// <summary>
    /// Sample for DigitalTwinInterfaceClient implementation.
    /// </summary>
    public class EnvironmentalSensorInterface : DigitalTwinInterfaceClient
    {
        public const string EnvironmentalSensorInterfaceId = "urn:csharp_sdk_sample:EnvironmentalSensor:1";
        private const string DeviceState = "state";
        private const string CustomerName = "name";
        private const string Brightness = "brightness";
        private const string Temperature = "temp";
        private const string Humidity = "humid";
        private const string BlinkCommand = "blink";
        private const string TurnOnLightCommand = "turnon";
        private const string TurnOffLightCommand = "turnoff";
        private const string RunDiagnosticsCommand = "rundiagnostics";

        /// <summary>
        /// Initializes a new instance of the <see cref="EnvironmentalSensorInterface"/> class.
        /// </summary>
        /// <param name="interfaceName">interface name.</param>
        public EnvironmentalSensorInterface(string interfaceName)
            : base(EnvironmentalSensorInterfaceId, interfaceName)
        {
        }

        /// <summary>
        /// Sample for reporting a property on an interface.
        /// </summary>
        /// <param name="state">state property.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task DeviceStatePropertyAsync(bool state)
        {
            var deviceStateProperty = new DigitalTwinPropertyReport(DeviceState, state.ToString().ToLower());
            await this.ReportPropertiesAsync(new Collection<DigitalTwinPropertyReport> { deviceStateProperty }).ConfigureAwait(false);
        }

        /// <summary>
        /// Send Temperature telemetry.
        /// </summary>
        /// <param name="temperature">telemetry value.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendTemperatureAsync(double temperature)
        {
            await this.SendTelemetryAsync(Temperature, temperature.ToString()).ConfigureAwait(false);
        }

        /// <summary>
        /// Send Humidity telemetry.
        /// </summary>
        /// <param name="humidity">telemetry value.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task SendHumidityAsync(double humidity)
        {
            await this.SendTelemetryAsync(Humidity, humidity.ToString()).ConfigureAwait(false);
        }


        /// <summary>
        /// Callback on command received.
        /// </summary>
        /// <param name="commandRequest">information regarding the command received.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task<DigitalTwinCommandResponse> OnCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            Console.WriteLine($"\t Command - {commandRequest.Name} was invoked from the service");
            Console.WriteLine($"\t Data - {commandRequest.Payload}");
            Console.WriteLine($"\t Request Id - {commandRequest.RequestId}.");

            switch (commandRequest.Name)
            {
                case BlinkCommand:
                    return new DigitalTwinCommandResponse(StatusCodeCompleted, "{\"description\": \"abc\"}");
                case RunDiagnosticsCommand:
                    var t = Task.Run(async () =>
                    {
                        Console.WriteLine("RunDiagnosticAsync started...");

                        // delay thread to simulate a long running operation
                        await Task.Delay(5 * 1000).ConfigureAwait(false);
                        string updateMessage = "25% complete";
                        Console.WriteLine(updateMessage);
                        await this.UpdateAsyncCommandStatusAsync(new DigitalTwinAsyncCommandUpdate(commandRequest.Name, commandRequest.RequestId, StatusCodePending, updateMessage)).ConfigureAwait(false);

                        await Task.Delay(5 * 1000).ConfigureAwait(false);
                        updateMessage = "50% complete";
                        Console.WriteLine(updateMessage);
                        await this.UpdateAsyncCommandStatusAsync(new DigitalTwinAsyncCommandUpdate(commandRequest.Name, commandRequest.RequestId, StatusCodePending, updateMessage)).ConfigureAwait(false);

                        await Task.Delay(5 * 1000).ConfigureAwait(false);
                        updateMessage = "75% complete";
                        Console.WriteLine(updateMessage);
                        await this.UpdateAsyncCommandStatusAsync(new DigitalTwinAsyncCommandUpdate(commandRequest.Name, commandRequest.RequestId, StatusCodePending, updateMessage)).ConfigureAwait(false);

                        await Task.Delay(5 * 1000).ConfigureAwait(false);
                        updateMessage = "100% complete";
                        Console.WriteLine(updateMessage);
                        Console.WriteLine("RunDiagnosticAsync done... Send status update.");
                        await this.UpdateAsyncCommandStatusAsync(new DigitalTwinAsyncCommandUpdate(commandRequest.Name, commandRequest.RequestId, StatusCodeCompleted, updateMessage)).ConfigureAwait(false);
                    });
                    return new DigitalTwinCommandResponse(StatusCodePending, null);
                case TurnOffLightCommand:
                    await this.DeviceStatePropertyAsync(false);
                    return new DigitalTwinCommandResponse(StatusCodeCompleted, null);
                case TurnOnLightCommand:
                    await this.DeviceStatePropertyAsync(true);
                    return new DigitalTwinCommandResponse(StatusCodeCompleted, null);
                default:
                    Console.WriteLine($"Command name '{commandRequest.Name}' is not handled.");
                    return new DigitalTwinCommandResponse(StatusCodeNotImplemented, null);
            }
        }

        /// <summary>
        /// Callback on property updated.
        /// </summary>
        /// <param name="propertyUpdate">information regarding the property updated.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        protected override async Task OnPropertyUpdated(DigitalTwinPropertyUpdate propertyUpdate)
        {
            Console.WriteLine($"Received updates for property '{propertyUpdate.PropertyName}'");

            switch (propertyUpdate.PropertyName)
            {
                case CustomerName:
                    await this.SetCustomerNameAsync(propertyUpdate).ConfigureAwait(false);
                    break;
                case Brightness:
                    await this.SetBrightnessAsync(propertyUpdate).ConfigureAwait(false);
                    break;
                default:
                    Console.WriteLine($"Property name '{propertyUpdate.PropertyName}' is not handled.");
                    break;
            }
        }

        /// <summary>
        /// Callback when registration is completed.
        /// </summary>
        protected override void OnRegistrationCompleted()
        {
            Console.WriteLine($"OnRegistrationCompleted.");
        }

        /// <summary>
        /// Process CustomerName property updated.
        /// </summary>
        /// <param name="customerNameUpdate">information of property to be reported.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SetCustomerNameAsync(DigitalTwinPropertyUpdate customerNameUpdate)
        {
            // code to consume customer value, currently just displaying on screen.
            string customerName = customerNameUpdate.PropertyDesired;
            Console.WriteLine($"Desired customer name = '{customerName}'.");
            Console.WriteLine($"Reported customer name = '{customerNameUpdate.PropertyReported}'.");
            Console.WriteLine($"Version is '{customerNameUpdate.DesiredVersion}'.");

            // report Completed
            var propertyReport = new Collection<DigitalTwinPropertyReport>();
            propertyReport.Add(new DigitalTwinPropertyReport(
                customerNameUpdate.PropertyName,
                customerNameUpdate.PropertyDesired,
                new DigitalTwinPropertyResponse(customerNameUpdate.DesiredVersion, StatusCodeCompleted, "Processing Completed")));
            await this.ReportPropertiesAsync(propertyReport).ConfigureAwait(false);
            Console.WriteLine("Sent completed status.");
        }

        /// <summary>
        /// Process Brightness property updated.
        /// </summary>
        /// <param name="brightnessUpdate">information of property to be reported.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private async Task SetBrightnessAsync(DigitalTwinPropertyUpdate brightnessUpdate)
        {
            // code to consume light brightness value, currently just displaying on screen
            string brightness = brightnessUpdate.PropertyDesired;
            long current = 0;

            Console.WriteLine($"Desired brightness = '{brightness}'.");
            Console.WriteLine($"Reported brightness = '{brightnessUpdate.PropertyReported}'.");
            Console.WriteLine($"Version is '{brightnessUpdate.DesiredVersion}'.");

            // report Pending
            var propertyReport = new Collection<DigitalTwinPropertyReport>();
            propertyReport.Add(new DigitalTwinPropertyReport(
                brightnessUpdate.PropertyName,
                current.ToString(),
                new DigitalTwinPropertyResponse(brightnessUpdate.DesiredVersion, 102, "Processing Request")));
            await this.ReportPropertiesAsync(propertyReport).ConfigureAwait(false);
            Console.WriteLine("Sent pending status for brightness property.");
            propertyReport.Clear();

            // Pretend calling command to Sensor to update brightness
            await Task.Delay(5 * 1000).ConfigureAwait(false);

            // report Completed
            propertyReport.Add(new DigitalTwinPropertyReport(
                brightnessUpdate.PropertyName,
                brightnessUpdate.PropertyDesired,
                new DigitalTwinPropertyResponse(
                    brightnessUpdate.DesiredVersion,
                    StatusCodeCompleted,
                    "Request completed")));
            await this.ReportPropertiesAsync(propertyReport).ConfigureAwait(false);
            Console.WriteLine("Sent completed status for brightness property.");
        }
    }
}
