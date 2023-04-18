// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Mash.Logging;
using Newtonsoft.Json;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class ModuleOperations
    {
        private readonly IotHubServiceClient _serviceClient;
        private readonly string _deviceId;
        private readonly string _moduleId;
        private readonly Logger _logger;

        private long _totalMethodCallsCount = 0;
        private long _totalDesiredPropertiesUpdatesCount = 0;

        private static readonly TimeSpan s_directMethodInvokeInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_desiredPropertiesSetInterval = TimeSpan.FromSeconds(3);

        private static readonly TimeSpan s_retryInterval = TimeSpan.FromSeconds(1);

        public ModuleOperations(IotHubServiceClient serviceClient, string deviceId, string moduleId, Logger logger)
        {
            _serviceClient = serviceClient;
            _deviceId = deviceId;
            _moduleId = moduleId;
            _logger = logger;
            _logger.LoggerContext.Add("deviceId", deviceId);
            _logger.LoggerContext.Add("moduleId", moduleId);
        }

        public async Task InvokeDirectMethodAsync(Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, DirectMethod);
            var sw = new Stopwatch();
            while (!ct.IsCancellationRequested)
            {
                var payload = new CustomDirectMethodPayload
                {
                    RandomId = Guid.NewGuid(),
                    CurrentTimeUtc = DateTimeOffset.UtcNow,
                    MethodCallsCount = ++_totalMethodCallsCount,
                };

                var methodInvocation = new DirectMethodServiceRequest("EchoPayload")
                {
                    Payload = payload,
                    ResponseTimeout = TimeSpan.FromSeconds(30),
                };

                logger.Trace($"Invoking direct method for device: {_deviceId}, module: {_moduleId}", TraceSeverity.Information);
                logger.Metric(TotalDirectMethodCallsToModuleCount, _totalMethodCallsCount);

                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        sw.Restart();
                        // Invoke the direct method asynchronously and get the response from the simulated module.
                        DirectMethodClientResponse response = await _serviceClient.DirectMethods
                            .InvokeAsync(_deviceId, _moduleId, methodInvocation, ct)
                            .ConfigureAwait(false);
                        sw.Stop();
                        logger.Metric(DirectMethodToModuleRoundTripSeconds, sw.Elapsed.TotalSeconds);

                        if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
                        {
                            logger.Metric(
                                M2cDirectMethodDelaySeconds,
                                (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
                        }

                        logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);
                        break;
                    }
                    catch (IotHubServiceException ex) when (ex.ErrorCode == IotHubServiceErrorCode.DeviceNotOnline)
                    {
                        logger.Trace($"Caught exception invoking direct method.\n{ex}", TraceSeverity.Warning);
                        // retry
                    }
                    catch (Exception ex)
                    {
                        logger.Trace($"Unexpected exception observed while invoking direct method.\n{ex}");
                        break;
                    }

                    // retry delay
                    await Task.Delay(s_retryInterval, ct).ConfigureAwait(false);
                }

                // interval delay
                await Task.Delay(s_directMethodInvokeInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SetDesiredPropertiesAsync(string keyName, string properties, Logger logger, CancellationToken ct)
        {
            logger.LoggerContext.Add(OperationName, SetDesiredProperties);
            var sw = new Stopwatch();

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    var twin = new ClientTwin();
                    twin.Properties.Desired[keyName] = properties;

                    ++_totalDesiredPropertiesUpdatesCount;
                    logger.Trace($"Updating the desired properties for device: {_deviceId}, module: {_moduleId}", TraceSeverity.Information);
                    logger.Metric(TotalDesiredPropertiesUpdatesToModuleCount, _totalDesiredPropertiesUpdatesCount);

                    sw.Restart();
                    await _serviceClient.Twins.UpdateAsync(_deviceId, _moduleId, twin, false, ct).ConfigureAwait(false);
                    sw.Stop();
                    logger.Metric(DesiredTwinUpdateToModuleRoundTripSeconds, sw.Elapsed.TotalSeconds);
                }
                catch (Exception ex)
                {
                    _logger.Trace($"Unexpected exception observed while requesting twin property update.\n{ex}");
                }

                await Task.Delay(s_desiredPropertiesSetInterval, ct).ConfigureAwait(false);
            }
        }
    }
}
