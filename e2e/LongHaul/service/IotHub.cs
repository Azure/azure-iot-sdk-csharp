// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Newtonsoft.Json;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class IotHub : IDisposable
    {
        private readonly Logger _logger;
        private readonly string _hubConnectionString;
        private readonly IotHubTransportProtocol _transportProtocol;
        private readonly string _deviceId;

        private static readonly TimeSpan s_interval = TimeSpan.FromSeconds(3);

        private static IotHubServiceClient s_serviceClient;

        public IotHub(Logger logger, string hubConnectionString, string deviceId, IotHubTransportProtocol transportProtocol)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubConnectionString = hubConnectionString;
            _deviceId = deviceId;
            _transportProtocol = transportProtocol;
        }

        /// <summary>
        /// Initializes the service client.
        /// </summary>
        public void Initialize()
        {
            var options = new IotHubServiceClientOptions
            {
                Protocol = _transportProtocol,
            };
            s_serviceClient = new IotHubServiceClient(_hubConnectionString, options);
            _logger.Trace("Initialized a new service client instance.", TraceSeverity.Information);
        }

        /// <summary>
        /// Runs a background.
        /// </summary>
        /// <param name="ct">The cancellation token</param>
        public async Task RunAsync(CancellationToken ct)
        {
            int methodCallsCount = 0;

            while (!ct.IsCancellationRequested)
            {
                await InvokeDirectMethodAsync(++methodCallsCount, ct).ConfigureAwait(false);
                await SetDesiredPropertiesAsync("methodCallsCount", methodCallsCount.ToString(), ct).ConfigureAwait(false);

                await Task.Delay(s_interval, ct).ConfigureAwait(false);
            }
        }

        public Task<string> GetEventHubCompatibleConnectionStringAsync(CancellationToken ct)
        {
            return s_serviceClient.GetEventHubCompatibleConnectionStringAsync(_hubConnectionString, ct);
        }

        public void Dispose()
        {
            _logger.Trace("Disposing", TraceSeverity.Verbose);

            s_serviceClient?.Dispose();

            _logger.Trace($"IotHub instance disposed", TraceSeverity.Verbose);
        }

        private async Task InvokeDirectMethodAsync(int methodCallsCount, CancellationToken ct)
        {
            var payload = new CustomDirectMethodPayload
            {
                RandomId = Guid.NewGuid(),
                CurrentTimeUtc = DateTimeOffset.UtcNow,
                MethodCallsCount = methodCallsCount,
            };

            var methodInvocation = new DirectMethodServiceRequest("EchoPayload")
            {
                Payload = payload,
                ResponseTimeout = TimeSpan.FromSeconds(30),
            };

            _logger.Trace($"Invoking direct method for device: {_deviceId}", TraceSeverity.Information);

            // Invoke the direct method asynchronously and get the response from the simulated device.
            DirectMethodClientResponse response = await s_serviceClient.DirectMethods.InvokeAsync(_deviceId, methodInvocation, ct);

            if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
            {
                _logger.Metric(
                    D2cDirectMethodDelaySeconds,
                    (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
            }

            _logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);
        }

        private async Task SetDesiredPropertiesAsync(string keyName, string properties, CancellationToken ct)
        {
            var twin = new ClientTwin();
            twin.Properties.Desired[keyName] = properties;
            await s_serviceClient.Twins.UpdateAsync(_deviceId, twin, false, ct).ConfigureAwait(false);
        }
    }
}
