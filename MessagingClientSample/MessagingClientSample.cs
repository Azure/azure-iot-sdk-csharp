// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample demonstrates how to send cloud-to-device messages. It also demonstrates the
    /// recommended pattern for handling connection loss events so that your application is 
    /// resilient to network instability.
    /// </summary>
    public class MessagingClientSample
    {
        private readonly IotHubServiceClient _hubClient;
        private readonly string _deviceId;

        public MessagingClientSample(IotHubServiceClient hubClient, string deviceId)
        {
            _hubClient = hubClient ?? throw new ArgumentNullException(nameof(hubClient));
            _deviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
        }

        public async Task SendMessagesAsync()
        {
            //TODO remove this
            await _hubClient.Messages.PurgeMessageQueueAsync(_deviceId);

            _hubClient.Messages.ErrorProcessor = OnConnectionLost;
            Console.WriteLine($"Opening the messaging client...");
            await _hubClient.Messages.OpenAsync();

            try
            {
                while (true)
                {
                    var message = new OutgoingMessage("My cloud to device message");
                    string correlationId = Guid.NewGuid().ToString();
                    message.CorrelationId = correlationId;

                    try
                    {
                        await _hubClient.Messages.SendAsync(_deviceId, message);
                        Console.WriteLine($"Successfully sent message with correlation Id {correlationId}");
                    }
                    catch
                    {
                        // Likely because the connection was dropped. The "OnConnectionLost" handler will handle
                        // re-establishing the connection
                        Console.WriteLine($"Failed to send message with correlation Id {correlationId}");
                    }

                    Console.WriteLine("Waiting a bit before sending the next message...");
                    Task.Delay(1000).Wait();
                }
            }
            finally
            {
                Console.WriteLine($"Closing the messaging client..."); 
                await _hubClient.Messages.CloseAsync();
            }
        }

        public async Task OnConnectionLost(ErrorContext errorContext)
        {
            if (errorContext.IOException != null)
            {
                Console.WriteLine($"Encountered a network error while sending messages: {errorContext.IOException.Message}");
            }
            else
            { 
                Console.WriteLine($"Encountered an IoT hub level error while sending messages. " +
                    $"Error code: {errorContext.IotHubServiceException.ErrorCode}, " +
                    $"Error message: {errorContext.IotHubServiceException.Message}");
            }

            Console.WriteLine("Attempting to re-open the connection");
            while (true)
            {
                try
                {
                    using var tokenSource = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                    await _hubClient.Messages.CloseAsync(tokenSource.Token);
                    await _hubClient.Messages.OpenAsync(tokenSource.Token);
                    
                    // The client was successfully re-opened, so the reconnection logic can end
                    return;
                }
                catch (Exception e)
                { 
                    Console.WriteLine($"Failed to re-open the connection. Error message: {e.Message}");
                    Console.WriteLine("Trying again...");
                }
            }
        }
    }
}
