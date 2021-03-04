// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    public class ServiceClientSample
    {
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);
        private static readonly TimeSpan s_operationTimeout = TimeSpan.FromSeconds(10);

        private static ServiceClient _serviceClient;
        private readonly string _hubConnectionString;
        private readonly TransportType _transportType;
        private readonly string _deviceId;
        private readonly ILogger _logger;

        public ServiceClientSample(string hubConnectionString, TransportType transportType, string deviceId, ILogger logger)
        {
            _hubConnectionString = hubConnectionString ?? throw new ArgumentNullException(nameof(hubConnectionString));
            _transportType = transportType;
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _logger = logger;
        }

        public async Task RunSampleAsync(TimeSpan runningTime)
        {
            using var cts = new CancellationTokenSource(runningTime);
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                cts.Cancel();
                _logger.LogInformation("Sample execution cancellation requested; will exit.");
            };

            try
            {
                await InitializeServiceClientAsync();
                await SendC2dMessagesAsync(cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }

        }

        private async Task SendC2dMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                var str = $"Hello, Cloud! - Message {++messageCount }";
                var message = new Message(Encoding.ASCII.GetBytes(str));
                _logger.LogInformation($"Sending C2D message {messageCount} with Id {message.MessageId} to {_deviceId}.");

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await _serviceClient.SendAsync(_deviceId, message, s_operationTimeout);
                        _logger.LogInformation($"Sent message {messageCount} with Id {message.MessageId} to {_deviceId}.");
                        message.Dispose();
                        break;
                    }
                    catch (Exception e) when (ExceptionHelper.IsNetwork(e))
                    {
                        _logger.LogError($"Transient Exception occurred, will retry: {e}");
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Unexpected error, will need to reinitialize the client: {e}");
                        await InitializeServiceClientAsync();
                    }
                    await Task.Delay(s_sleepDuration);
                }
                await Task.Delay(s_sleepDuration);
            }
        }

        private async Task InitializeServiceClientAsync()
        {
            if (_serviceClient != null)
            {
                await _serviceClient.CloseAsync();
                _serviceClient.Dispose();
                _serviceClient = null;
                _logger.LogInformation("Closed and disposed the current service client instance.");
            }

            var options = new ServiceClientOptions
            {
                SdkAssignsMessageId = Shared.SdkAssignsMessageId.WhenUnset,
            };
            _serviceClient = ServiceClient.CreateFromConnectionString(_hubConnectionString, _transportType, options);
            _logger.LogInformation("Initialized a new service client instance.");
        }
    }
}
