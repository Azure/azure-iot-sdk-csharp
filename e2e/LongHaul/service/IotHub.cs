// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Newtonsoft.Json;

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
            while (!ct.IsCancellationRequested)
            {
                var payload = new KeyValuePair<string, string>("Guid_Value", Guid.NewGuid().ToString());
                var methodInvocation = new DirectMethodServiceRequest("EchoPayload")
                {
                    Payload = payload,
                    ResponseTimeout = TimeSpan.FromSeconds(30),
                };

                _logger.Trace($"Invoking direct method for device: {_deviceId}", TraceSeverity.Information);

                // Invoke the direct method asynchronously and get the response from the simulated device.
                DirectMethodClientResponse response = await s_serviceClient.DirectMethods.InvokeAsync(_deviceId, methodInvocation);

                _logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);

                await Task.Delay(s_interval, ct).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _logger.Trace("Disposing", TraceSeverity.Verbose);

            s_serviceClient?.Dispose();

            _logger.Trace($"IotHub instance disposed", TraceSeverity.Verbose);
        }
    }
}
