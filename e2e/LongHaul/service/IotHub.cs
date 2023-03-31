// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Mash.Logging;
using Azure.Storage.Blobs;
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
        private int _totalFileUploadNotificationsReceived;

        private static IotHubServiceClient s_serviceClient;
        private BlobContainerClient _blobContainerClient;

        private static volatile Dictionary<string, Action> s_onlineDeviceOperations;

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
            s_onlineDeviceOperations = new Dictionary<string, Action>();
            _logger.Trace("Initialized a new service client instance.", TraceSeverity.Information);

            _totalFileUploadNotificationsReceived = 0;
            _blobContainerClient = new BlobContainerClient(_storageConnectionString, "fileupload");
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

                    if (s_onlineDeviceOperations.ContainsKey(deviceId) && device.ConnectionState is ClientConnectionState.Disconnected)
                    {
                        s_onlineDeviceOperations.Remove(deviceId);
                    }
                    else if (!s_onlineDeviceOperations.ContainsKey(deviceId) && device.ConnectionState is ClientConnectionState.Connected)
                    {
                        s_onlineDeviceOperations.Add(
                            deviceId,
                            async () =>
                            {
                                using var deviceOperations = new DeviceOperations(s_serviceClient, deviceId, _logger);
                                _logger.Trace($"Creating {nameof(DeviceOperations)} on the device [{deviceId}]", TraceSeverity.Verbose);

                                await Task
                                    .WhenAll(
                                        deviceOperations.InvokeDirectMethodAsync(ct),
                                        deviceOperations.SetDesiredPropertiesAsync("guidValue", Guid.NewGuid().ToString(), ct),
                                        deviceOperations.SendC2dMessagesAsync(ct))
                                    .ConfigureAwait(false);
                            });

                        s_onlineDeviceOperations[deviceId]?.Invoke();
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
            _logger.Trace("Starting to listen to cloud-to-device feedback messages", TraceSeverity.Verbose);

            AcknowledgementType OnC2dMessageAck(FeedbackBatch feedbackMessages)
            {
                foreach (FeedbackRecord feedbackRecord in feedbackMessages.Records)
                {
                    _logger.Trace(
                        $"Device {feedbackRecord.DeviceId} acted on message: {feedbackRecord.OriginalMessageId} with status: {feedbackRecord.StatusCode}",
                        TraceSeverity.Information);
                }

                _totalFeedbackMessagesReceivedCount += feedbackMessages.Records.Count();
                _logger.Metric(TotalFeedbackMessagesReceivedCount, _totalFeedbackMessagesReceivedCount);

                return AcknowledgementType.Complete;
            }

            s_serviceClient.MessageFeedback.MessageFeedbackProcessor = OnC2dMessageAck;

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

        public async Task ReceiveFileUploadAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                await s_serviceClient.FileUploadNotifications.OpenAsync(ct).ConfigureAwait(false);
                _logger.Trace("Listening for file upload notifications from the service...", TraceSeverity.Verbose);

                Task<AcknowledgementType> FileUploadNotificationCallback(FileUploadNotification fileUploadNotification)
                {
                    AcknowledgementType ackType = AcknowledgementType.Complete;
                    _totalFileUploadNotificationsReceived++;

                    var sb = new StringBuilder();
                    sb.Append($"Received file upload notification.");
                    sb.Append($"\tDeviceId: {fileUploadNotification.DeviceId ?? "N/A"}.");
                    sb.Append($"\tFileName: {fileUploadNotification.BlobName ?? "N/A"}.");
                    sb.Append($"\tEnqueueTimeUTC: {fileUploadNotification.EnqueuedOnUtc}.");
                    sb.Append($"\tBlobSizeInBytes: {fileUploadNotification.BlobSizeInBytes}.");
                    _logger.Trace(sb.ToString(), TraceSeverity.Information);

                    _blobContainerClient.DeleteBlobIfExists(fileUploadNotification.BlobName);
                    return Task.FromResult(ackType);
                }

                s_serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = FileUploadNotificationCallback;
                _logger.Metric("TotalFileUploadNotificiationsReceived", _totalFileUploadNotificationsReceived);

                await Task.Delay(s_receiveFileUploadInterval, ct).ConfigureAwait(false);
            }
        }

        public void Dispose()
        {
            _logger.Trace("Disposing", TraceSeverity.Verbose);

            s_serviceClient?.Dispose();

            _logger.Trace($"IoT Hub instance disposed", TraceSeverity.Verbose);
        }
    }
}
