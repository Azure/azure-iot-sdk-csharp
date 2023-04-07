// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using Azure;
using Mash.Logging;
using Azure.Storage.Blobs;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class IotHub : IDisposable
    {
        private readonly Logger _logger;
        private readonly string _hubConnectionString;
        private readonly IotHubTransportProtocol _transportProtocol;
        private readonly string _storageConnectionString;

        private static readonly TimeSpan s_deviceCountMonitorInterval = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan s_receiveFileUploadInterval = TimeSpan.FromSeconds(30);

        private static IotHubServiceClient s_serviceClient;
        private static BlobContainerClient s_blobContainerClient;

        // Create a mapping between a device Id and a tuple of the correlated operations with the cancellation token source per online device.
        private static ConcurrentDictionary<string, Tuple<Task, CancellationTokenSource>> s_onlineDeviceOperations;

        private long _totalFileUploadNotificationsReceived = 0;
        private long _totalFeedbackMessagesReceivedCount = 0;

        public IotHub(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubConnectionString = parameters.IotHubConnectionString;
            _transportProtocol = parameters.TransportProtocol;
            _storageConnectionString = parameters.StorageConnectionString;
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

            s_onlineDeviceOperations = new ConcurrentDictionary<string, Tuple<Task, CancellationTokenSource>>();
            s_blobContainerClient = new BlobContainerClient(_storageConnectionString, "fileupload");
        }

        public Task<string> GetEventHubCompatibleConnectionStringAsync(CancellationToken ct)
        {
            return s_serviceClient.GetEventHubCompatibleConnectionStringAsync(_hubConnectionString, ct);
        }

        public async Task MonitorConnectedDevicesAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                AsyncPageable<ClientTwin> allDevices = s_serviceClient.Query.Create<ClientTwin>(
                    "SELECT deviceId, connectionState, lastActivityTime FROM devices where is_defined(properties.reported.runId)",
                    ct);

                await foreach (ClientTwin device in allDevices)
                {
                    string deviceId = device.DeviceId;

                    if (s_onlineDeviceOperations.ContainsKey(deviceId)
                        && device.ConnectionState is ClientConnectionState.Disconnected)
                    {
                        CancellationTokenSource source = s_onlineDeviceOperations[deviceId].Item2;
                        // Signal cancellation to all tasks on the particular device.
                        source.Cancel();
                        // Dispose the cancellation token source.
                        source.Dispose();
                        // Remove the correlated device operations and cancellation token source of the particular device from the dictionary.
                        s_onlineDeviceOperations.TryRemove(deviceId, out _);
                    }
                    else if (!s_onlineDeviceOperations.ContainsKey(deviceId)
                        && device.ConnectionState is ClientConnectionState.Connected)
                    {
                        // For each online device, initiate a new cancellation token source.
                        // Once the device goes offline, cancel all operations on this device.
                        var source = new CancellationTokenSource();
                        CancellationToken token = source.Token;

                        async Task Operations()
                        {
                            var deviceOperations = new DeviceOperations(s_serviceClient, deviceId);
                            _logger.Trace($"Creating {nameof(DeviceOperations)} on the device [{deviceId}]", TraceSeverity.Verbose);

                            Logger loggerPerDevice = _logger.Clone();
                            loggerPerDevice.LoggerContext.Add("DeviceId", deviceId);

                            try
                            {
                                await Task
                                .WhenAll(
                                    deviceOperations.InvokeDirectMethodAsync(loggerPerDevice.Clone(), token),
                                    deviceOperations.SetDesiredPropertiesAsync("guidValue", Guid.NewGuid().ToString(), loggerPerDevice.Clone(), token),
                                    deviceOperations.SendC2dMessagesAsync(loggerPerDevice.Clone(), token))
                                .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.Trace($"Operations on [{deviceId}] have been canceled as the device goes offline.", TraceSeverity.Information);
                            }
                            catch (Exception ex)
                            {
                                _logger.Trace($"Service app failed with exception {ex}", TraceSeverity.Error);
                            }
                        }

                        // Passing in "Operations()" as Task so we don't need to manually call "Invoke()" on it.
                        var operationsTuple = new Tuple<Task, CancellationTokenSource>(Operations(), source);
                        s_onlineDeviceOperations.TryAdd(deviceId, operationsTuple);
                    }
                }
                _logger.Trace($"Total number of connected devices: {s_onlineDeviceOperations.Count}", TraceSeverity.Information);
                _logger.Metric(TotalOnlineDevicesCount, s_onlineDeviceOperations.Count);

                await Task.Delay(s_deviceCountMonitorInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task ReceiveMessageFeedbacksAsync(CancellationToken ct)
        {
            // It is important to note that receiver only gets feedback messages when the device is actively running and acting on messages.
            _logger.Trace("Starting to listen to cloud-to-device feedback messages...", TraceSeverity.Verbose);

            Task<AcknowledgementType> OnC2dFeedback(FeedbackBatch feedbackMessages)
            {
                foreach (FeedbackRecord feedbackRecord in feedbackMessages.Records)
                {
                    _logger.Trace(
                        $"Device {feedbackRecord.DeviceId} acted on message: {feedbackRecord.OriginalMessageId} with status: {feedbackRecord.StatusCode}",
                        TraceSeverity.Information);
                }

                _totalFeedbackMessagesReceivedCount += feedbackMessages.Records.Count();
                _logger.Metric(TotalFeedbackMessagesReceivedCount, _totalFeedbackMessagesReceivedCount);

                return Task.FromResult(AcknowledgementType.Complete);
            }

            async Task OnC2dFeedbackError(ErrorContext errorContext)
            {
                Exception exToLog = errorContext.IotHubServiceException == null
                    ? errorContext.IOException
                    : errorContext.IotHubServiceException;
                _logger.Trace($"Error processing C2D feedback messages.\n{exToLog}");

                await s_serviceClient.MessageFeedback.OpenAsync(ct).ConfigureAwait(false);
            }

            s_serviceClient.MessageFeedback.MessageFeedbackProcessor = OnC2dFeedback;
            s_serviceClient.MessageFeedback.ErrorProcessor = OnC2dFeedbackError;

            try
            {
                await s_serviceClient.MessageFeedback.OpenAsync(ct).ConfigureAwait(false);

                try
                {
                    await Task.Delay(-1, ct);
                }
                catch (OperationCanceledException) { }
            }
            finally
            {
                await s_serviceClient.MessageFeedback.CloseAsync().ConfigureAwait(false);
            }
        }

        public async Task ReceiveFileUploadNotificationsAsync(CancellationToken ct)
        {
            _logger.Trace("Starting to listen to file upload notifications...", TraceSeverity.Verbose);

            async Task<AcknowledgementType> FileUploadNotificationCallback(FileUploadNotification fileUploadNotification)
            {
                ++_totalFileUploadNotificationsReceived;
                _logger.Metric(TotalFileUploadNotificiationsReceivedCount, _totalFileUploadNotificationsReceived);

                var sb = new StringBuilder("Received file upload notification.");
                if (!string.IsNullOrWhiteSpace(fileUploadNotification.DeviceId))
                {
                    sb.Append($"\n\tDeviceId: {fileUploadNotification.DeviceId}.");
                }
                if (!string.IsNullOrWhiteSpace(fileUploadNotification.BlobName))
                {
                    sb.Append($"\n\tFileName: {fileUploadNotification.BlobName}.");
                }
                sb.Append($"\n\tEnqueuedOnUtc: {fileUploadNotification.EnqueuedOnUtc}.");
                sb.Append($"\n\tBlobSizeInBytes: {fileUploadNotification.BlobSizeInBytes}.");
                _logger.Trace(sb.ToString(), TraceSeverity.Information);

                await s_blobContainerClient.DeleteBlobIfExistsAsync(fileUploadNotification.BlobName).ConfigureAwait(false);
                return AcknowledgementType.Complete;
            }

            async Task FileUploadNotificationErrors(ErrorContext errorContext)
            {
                Exception exToLog = errorContext.IotHubServiceException == null
                    ? errorContext.IOException
                    : errorContext.IotHubServiceException;
                _logger.Trace($"Error processing FileUploadNotification.\n{exToLog}");

                _logger.Trace("Attempting reconnect for FileUploadNotifications...");
                await s_serviceClient.FileUploadNotifications.OpenAsync(ct).ConfigureAwait(false);
                _logger.Trace("Reconnected for FileUploadNotifications.");
            }

            s_serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = FileUploadNotificationCallback;
            s_serviceClient.FileUploadNotifications.ErrorProcessor = FileUploadNotificationErrors;

            try
            {
                await s_serviceClient.FileUploadNotifications.OpenAsync(ct).ConfigureAwait(false);

                try
                {
                    await Task.Delay(-1, ct);
                }
                catch (OperationCanceledException) { }
            }
            finally
            {
                await s_serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _logger.Trace("Disposing IotHub instance...", TraceSeverity.Verbose);
            s_serviceClient?.Dispose();
            _logger.Trace("IotHub instance disposed.", TraceSeverity.Verbose);
        }
    }
}
