// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Samples
{
    internal class SimulatedDeviceWithCommand
    {
        private readonly TimeSpan? _maxRunTime;
        private readonly IotHubDeviceClient _deviceClient;
        private static TimeSpan s_telemetryInterval = TimeSpan.FromSeconds(1); // Seconds;

        public SimulatedDeviceWithCommand(IotHubDeviceClient deviceClient, TimeSpan? maxRunTime)
        {
            _deviceClient = deviceClient ?? throw new ArgumentNullException(nameof(deviceClient));
            _maxRunTime = maxRunTime;
        }

        public async Task RunSampleAsync()
        {
            using CancellationTokenSource cts = _maxRunTime.HasValue
                ? new CancellationTokenSource(_maxRunTime.Value)
                : new CancellationTokenSource();
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                Console.WriteLine("Sample execution cancellation requested; will exit.");
            };

            Console.WriteLine($"{DateTime.Now}> Press Control+C at any time to quit the sample.");

            try
            {
                await _deviceClient.OpenAsync(cts.Token);

                await _deviceClient.SetDirectMethodCallbackAsync(SetTelemetryInterval, cts.Token);

                await SendDeviceToCloudMessagesAsync(cts.Token);
            }
            catch (OperationCanceledException) { }
        }

        // Handle the direct method call.
        private Task<DirectMethodResponse> SetTelemetryInterval(DirectMethodRequest methodRequest)
        {
            // Check the payload is a single integer value.
            if (methodRequest.TryGetPayload<int>(out int telemetryIntervalInSeconds))
            {
                s_telemetryInterval = TimeSpan.FromSeconds(telemetryIntervalInSeconds);

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"Telemetry interval set to {s_telemetryInterval}");
                Console.ResetColor();

                // Acknowlege the direct method call with the status code 200.
                return Task.FromResult(new DirectMethodResponse(200));
            }
            else
            {
                // Acknowlege the direct method call the status code 400.
                return Task.FromResult(new DirectMethodResponse(400));
            }
        }

        // Async method to send simulated telemetry.
        private async Task SendDeviceToCloudMessagesAsync(CancellationToken ct)
        {
            // Initial telemetry values.
            double minTemperature = 20;
            double minHumidity = 60;
            var rand = new Random();

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    double currentTemperature = minTemperature + rand.NextDouble() * 15;
                    double currentHumidity = minHumidity + rand.NextDouble() * 20;

                    // Create message.
                    var telemetryDataPoint = new
                    {
                        temperature = currentTemperature,
                        humidity = currentHumidity
                    };
                    var message = new OutgoingMessage(telemetryDataPoint);

                    // Add a custom application property to the message.
                    // An IoT hub can filter on these properties without access to the message body.
                    message.Properties.Add("temperatureAlert", (currentTemperature > 30) ? "true" : "false");

                    // Send the telemetry message.
                    await _deviceClient.SendTelemetryAsync(message, ct);
                    Console.WriteLine($"{DateTime.Now} > Sending message: {JsonConvert.SerializeObject(telemetryDataPoint)}");

                    await Task.Delay(s_telemetryInterval, ct);
                }
            }
            catch (OperationCanceledException) { } // User canceled
        }
    }
}
