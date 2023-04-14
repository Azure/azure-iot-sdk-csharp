﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Storage.Blobs;
using Mash.Logging;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

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
        private static ConcurrentDictionary<string, Tuple<Task, CancellationTokenSource>> s_onlineModuleOperations;

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
            s_onlineModuleOperations = new ConcurrentDictionary<string, Tuple<Task, CancellationTokenSource>>();
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
                    "SELECT deviceId, connectionState FROM devices where is_defined(properties.reported.runId)",
                    ct);

                AsyncPageable<ClientTwin> allModules = s_serviceClient.Query.Create<ClientTwin>(
                    "SELECT deviceId, moduleId, connectionState FROM devices.modules where is_defined(properties.reported.runId)",
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
                            var deviceOperations = new DeviceOperations(s_serviceClient, deviceId, _logger.Clone());
                            _logger.Trace($"Creating {nameof(DeviceOperations)} on the device [{deviceId}]", TraceSeverity.Verbose);

                            try
                            {
                                await Task
                                .WhenAll(
                                    deviceOperations.InvokeDirectMethodAsync(_logger.Clone(), token),
                                    deviceOperations.SetDesiredPropertiesAsync("guidValue", Guid.NewGuid().ToString(), _logger.Clone(), token),
                                    deviceOperations.SendC2dMessagesAsync(_logger.Clone(), token))
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

                await foreach (ClientTwin module in allModules)
                {
                    string moduleId = module.DeviceId + "_" + module.ModuleId;
                    if (s_onlineModuleOperations.ContainsKey(moduleId)
                        && module.ConnectionState is ClientConnectionState.Disconnected)
                    {
                        CancellationTokenSource source = s_onlineModuleOperations[moduleId].Item2;
                        // Signal cancellation to all tasks on the particular device.
                        source.Cancel();
                        // Dispose the cancellation token source.
                        source.Dispose();
                        // Remove the correlated device operations and cancellation token source of the particular device from the dictionary.
                        s_onlineDeviceOperations.TryRemove(moduleId, out _);
                    }
                    else if (!s_onlineModuleOperations.ContainsKey(moduleId)
                        && module.ConnectionState is ClientConnectionState.Connected)
                    {
                        // For each online module, initiate a new cancellation token source.
                        // Once the module goes offline, cancel all operations on this module.
                        var source = new CancellationTokenSource();
                        CancellationToken token = source.Token;

                        async Task Operations()
                        {
                            var moduleOperations = new ModuleOperations(s_serviceClient, module.DeviceId, module.ModuleId, _logger.Clone());
                            _logger.Trace($"Creating {nameof(ModuleOperations)} on the device: [{module.DeviceId}], module: [{module.ModuleId}]", TraceSeverity.Verbose);

                            try
                            {
                                await Task
                                .WhenAll(
                                    moduleOperations.InvokeDirectMethodAsync(_logger.Clone(), token),
                                    moduleOperations.SetDesiredPropertiesAsync("guidValue", Guid.NewGuid().ToString(), _logger.Clone(), token))
                                .ConfigureAwait(false);
                            }
                            catch (OperationCanceledException)
                            {
                                _logger.Trace($"Operations on device: [{module.DeviceId}], module: [{module.ModuleId}] have been canceled as the device goes offline.", TraceSeverity.Information);
                            }
                            catch (Exception ex)
                            {
                                _logger.Trace($"Service app failed with exception {ex}", TraceSeverity.Error);
                            }
                        }

                        // Passing in "Operations()" as Task so we don't need to manually call "Invoke()" on it.
                        var operationsTuple = new Tuple<Task, CancellationTokenSource>(Operations(), source);
                        s_onlineDeviceOperations.TryAdd(moduleId, operationsTuple);
                    }
                }

                _logger.Trace($"Total number of connected devices: {s_onlineDeviceOperations.Count}", TraceSeverity.Information);
                _logger.Trace($"Total number of connected modules: {s_onlineModuleOperations.Count}", TraceSeverity.Information);
                _logger.Metric(TotalOnlineDevicesCount, s_onlineDeviceOperations.Count);
                _logger.Metric(TotalOnlineModulesCount, s_onlineModuleOperations.Count);

                await Task.Delay(s_deviceCountMonitorInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task ReceiveMessageFeedbacksAsync(CancellationToken ct)
        {
            // It is important to note that receiver only gets feedback messages when the device is actively running and acting on messages.
            _logger.Trace("Starting to listen to cloud-to-device feedback messages...", TraceSeverity.Verbose);

            Task<AcknowledgementType> OnC2dMessageAck(FeedbackBatch feedbackMessages)
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

            async Task OnC2dError(MessageFeedbackProcessorError error)
            {
                _logger.Trace($"Error processing C2D message.\n{error.Exception}");

                await s_serviceClient.MessageFeedback.OpenAsync(ct).ConfigureAwait(false);
            }

            s_serviceClient.MessageFeedback.MessageFeedbackProcessor = OnC2dMessageAck;
            s_serviceClient.MessageFeedback.ErrorProcessor = OnC2dError;

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

            async Task FileUploadNotificationErrors(FileUploadNotificationProcessorError error)
            {
                _logger.Trace($"Error processing FileUploadNotification.\n{error.Exception}");

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
