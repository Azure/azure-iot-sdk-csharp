// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample demonstrates how to send cloud-to-device messages. It also demonstrates the
    /// recommended pattern for handling connection loss events so that your application is 
    /// resilient to network instability. Please note that the service client instance passed into 
    /// this class has been configured to use retry logic.
    /// </summary>
    public class MessagingClientSample
    {
        private readonly IotHubServiceClient _hubClient;
        private readonly string _deviceId;
        private readonly ILogger _logger;

        public MessagingClientSample(IotHubServiceClient hubClient, string deviceId, ILogger logger)
        {
            _hubClient = hubClient ?? throw new ArgumentNullException(nameof(hubClient));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            _logger = logger ?? throw new ArgumentNullException(nameof(_logger));
        }

        public async Task SendMessagesAsync(CancellationToken cancellationToken)
        {
            // Set up the callback for handling any connection loss events
            _hubClient.Messages.ErrorProcessor = OnConnectionLost;

            _logger.LogInformation($"Opening the messaging client...");
            await _hubClient.Messages.OpenAsync(cancellationToken);

            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var message = new OutgoingMessage("My cloud to device message");
                    string correlationId = Guid.NewGuid().ToString();
                    message.CorrelationId = correlationId;

                    try
                    {
                        await _hubClient.Messages.SendAsync(_deviceId, message, cancellationToken);
                        _logger.LogInformation($"Successfully sent message with correlation Id {correlationId}");
                    }
                    catch (IotHubServiceException e)
                    {
                        // Likely because the connection was dropped. The "OnConnectionLost" handler will handle
                        // re-establishing the connection
                        _logger.LogError($"Failed to send message with correlation Id {correlationId} due to error {e.Message} and error code {e.ErrorCode}");
                    }
                    catch (Exception e)
                    {
                        // Likely because the connection was dropped. The "OnConnectionLost" handler will handle
                        // re-establishing the connection
                        _logger.LogError($"Failed to send message with correlation Id {correlationId} due to error {e.Message}");
                    }

                    _logger.LogInformation("Waiting a bit before sending the next message...");
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Operation cancelled, exiting sample...");
            }
            finally
            {
                _logger.LogInformation($"Closing the messaging client...");

                // Using a separate cancellation token here because the one provided to this function has already been cancelled
                using CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
                await _hubClient.Messages.CloseAsync(cts.Token);
            }
        }

        public async Task OnConnectionLost(MessagesClientError error)
        {
            _logger.LogError($"Encountered an error while sending messages. " +
                $"Error message: {error.Exception.Message}");

            // Note that this client has opted into using retry logic, so this open call will retry even if it fails.
            _logger.LogInformation("Attempting to re-open the connection");
            await _hubClient.Messages.OpenAsync();
            _logger.LogInformation("Successfully re-opened the connection");
        }
    }
}
