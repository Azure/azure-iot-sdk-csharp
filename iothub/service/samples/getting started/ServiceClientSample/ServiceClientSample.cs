// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    public class ServiceClientSample
    {
        private static readonly TimeSpan s_sleepDuration = TimeSpan.FromSeconds(5);

        private static IotHubServiceClient s_serviceClient;
        private readonly string _hubConnectionString;
        private readonly string _deviceId;
        private readonly ILogger _logger;

        public ServiceClientSample(string hubConnectionString, string deviceId, ILogger logger)
        {
            _hubConnectionString = hubConnectionString ?? throw new ArgumentNullException(nameof(hubConnectionString));
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
                InitializeServiceClient();
                Task sendTask = SendC2dMessagesAsync(cts.Token);
                Task receiveTask = ReceiveMessageFeedbacksAsync(cts.Token);

                await Task.WhenAll(sendTask, receiveTask);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, so exiting...: \n{ex}");
            }

        }

        private async Task ReceiveMessageFeedbacksAsync(CancellationToken token)
        {
            // It is important to note that receiver only gets feedback messages when the device is actively running and acting on messages.
            _logger.LogInformation("Starting to listen to feedback messages");

            AcknowledgementType OnC2dMessageAck(FeedbackBatch feedbackMessages)
            {
                AcknowledgementType ackType = AcknowledgementType.Abandon;

                var sb = new StringBuilder();

                sb.Append("New Feedback received:");
                sb.Append($"\tEnqueue Time: {feedbackMessages.EnqueuedOnUtc}");
                sb.Append($"\tNumber of messages in the batch: {feedbackMessages.Records.Count()}");
                _logger.LogInformation(sb.ToString());

                foreach (FeedbackRecord feedbackRecord in feedbackMessages.Records)
                {
                    _logger.LogInformation($"\tDevice {feedbackRecord.DeviceId} acted on message: {feedbackRecord.OriginalMessageId} with status: {feedbackRecord.StatusCode}");
                }

                return ackType;
            }

            s_serviceClient.MessageFeedback.MessageFeedbackProcessor = OnC2dMessageAck;
            await s_serviceClient.MessageFeedback.OpenAsync(token);

            try
            {
                await Task.Delay(-1, token);
            }
            catch (OperationCanceledException) { }

            await s_serviceClient.MessageFeedback.CloseAsync(token);
        }

        private async Task SendC2dMessagesAsync(CancellationToken cancellationToken)
        {
            int messageCount = 0;
            while (!cancellationToken.IsCancellationRequested)
            {
                string str = $"Hello, Cloud! - Message {++messageCount }";
                var message = new OutgoingMessage(str)
                {
                    // An acknowledgment is sent on delivery success or failure.
                    Ack = DeliveryAcknowledgement.Full
                };

                _logger.LogInformation($"Sending C2D message {messageCount} with Id {message.MessageId} to {_deviceId}.");
                await s_serviceClient.Messages.OpenAsync(cancellationToken);

                while (!cancellationToken.IsCancellationRequested)
                {
                    try
                    {
                        await s_serviceClient.Messages.SendAsync(_deviceId, message, cancellationToken);
                        _logger.LogInformation($"Sent message {messageCount} with Id {message.MessageId} to {_deviceId}.");
                        break;
                    }
                    catch (Exception e)
                    {
                        _logger.LogError($"Unexpected error, will need to reinitialize the client: {e}");
                        InitializeServiceClient();
                    }
                    await Task.Delay(s_sleepDuration, cancellationToken);
                }
                await Task.Delay(s_sleepDuration, cancellationToken);
                await s_serviceClient.Messages.CloseAsync(cancellationToken);
            }
        }

        private void InitializeServiceClient()
        {
            var options = new IotHubServiceClientOptions
            {
                Protocol = IotHubTransportProtocol.Tcp,
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            s_serviceClient = new IotHubServiceClient(_hubConnectionString, options);
            _logger.LogInformation("Initialized a new service client instance.");
        }
    }
}
