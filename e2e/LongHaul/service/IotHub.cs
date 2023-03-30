// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Mash.Logging;
using Newtonsoft.Json;
using static Microsoft.Azure.Devices.LongHaul.Service.LoggingConstants;

namespace Microsoft.Azure.Devices.LongHaul.Service
{
    internal class IotHub : IDisposable
    {
        private readonly Logger _logger;
        private readonly string _hubConnectionString;
        private readonly string _storageConnectionString;
        private readonly IotHubTransportProtocol _transportProtocol;
        private readonly string _deviceId;

        private static readonly TimeSpan s_directMethodInvokeInterval = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan s_desiredPropertiesSetInterval = TimeSpan.FromSeconds(3);
        private int _totalFileUploadNotificationsReceived;
        private int _totalFileUploadNotificationsCompleted;
        private int _totalFileUploadNotificationsAbandoned;

        private IotHubServiceClient _serviceClient;
        private BlobContainerClient _blobContainerClient;

        public IotHub(Logger logger, Parameters parameters)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _hubConnectionString = parameters.IotHubConnectionString;
            _deviceId = parameters.DeviceId;
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
            _serviceClient = new IotHubServiceClient(_hubConnectionString, options);
            _logger.Trace("Initialized a new service client instance.", TraceSeverity.Information);

            _totalFileUploadNotificationsAbandoned = 0;
            _totalFileUploadNotificationsReceived = 0;
            _totalFileUploadNotificationsCompleted = 0;
            _blobContainerClient = new BlobContainerClient(_storageConnectionString, "fileupload");
        }

        public Task<string> GetEventHubCompatibleConnectionStringAsync(CancellationToken ct)
        {
            return _serviceClient.GetEventHubCompatibleConnectionStringAsync(_hubConnectionString, ct);
        }

        public async Task InvokeDirectMethodAsync(CancellationToken ct)
        {
            int methodCallsCount = 0;

            while (!ct.IsCancellationRequested)
            {
                var payload = new CustomDirectMethodPayload
                {
                    RandomId = Guid.NewGuid(),
                    CurrentTimeUtc = DateTimeOffset.UtcNow,
                    MethodCallsCount = ++methodCallsCount,
                };

                var methodInvocation = new DirectMethodServiceRequest("EchoPayload")
                {
                    Payload = payload,
                    ResponseTimeout = TimeSpan.FromSeconds(30),
                };

                _logger.Trace($"Invoking direct method for device: {_deviceId}", TraceSeverity.Information);

                // Invoke the direct method asynchronously and get the response from the simulated device.
                DirectMethodClientResponse response = await _serviceClient.DirectMethods.InvokeAsync(_deviceId, methodInvocation, ct);

                if (response.TryGetPayload(out CustomDirectMethodPayload responsePayload))
                {
                    _logger.Metric(
                        D2cDirectMethodDelaySeconds,
                        (DateTimeOffset.UtcNow - responsePayload.CurrentTimeUtc).TotalSeconds);
                }

                _logger.Trace($"Response status: {response.Status}, payload:\n\t{JsonConvert.SerializeObject(response.PayloadAsString)}", TraceSeverity.Information);

                await Task.Delay(s_directMethodInvokeInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task SetDesiredPropertiesAsync(string keyName, string properties, CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                var twin = new ClientTwin();
                twin.Properties.Desired[keyName] = properties;
                await _serviceClient.Twins.UpdateAsync(_deviceId, twin, false, ct).ConfigureAwait(false);

                await Task.Delay(s_desiredPropertiesSetInterval, ct).ConfigureAwait(false);
            }
        }

        public async Task ReceiveFileUploadAsync(CancellationToken ct)
        {
            await _serviceClient.FileUploadNotifications.OpenAsync(ct).ConfigureAwait(false);
            _logger.Trace("Listening for file upload notifications from the service...");

            AcknowledgementType FileUploadNotificationCallback(FileUploadNotification fileUploadNotification)
            {
                AcknowledgementType ackType = AcknowledgementType.Complete;
                _totalFileUploadNotificationsReceived++;

                var sb = new StringBuilder();
                sb.Append($"Received file upload notification.");
                sb.Append($"\tDeviceId: {fileUploadNotification.DeviceId ?? "N/A"}.");
                sb.Append($"\tFileName: {fileUploadNotification.BlobName ?? "N/A"}.");
                sb.Append($"\tEnqueueTimeUTC: {fileUploadNotification.EnqueuedOnUtc}.");
                sb.Append($"\tBlobSizeInBytes: {fileUploadNotification.BlobSizeInBytes}.");
                _logger.Trace(sb.ToString());

                _blobContainerClient.DeleteBlobIfExists(fileUploadNotification.BlobName);
                return ackType;
            }

            _serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = FileUploadNotificationCallback;
            _logger.Trace($"Total File Notifications Received: {_totalFileUploadNotificationsReceived}.");

            await Task.Delay(30 * 1000);
        }

        public void Dispose()
        {
            _logger.Trace("Disposing", TraceSeverity.Verbose);

            _serviceClient.FileUploadNotifications.CloseAsync().ConfigureAwait(false);
            _serviceClient?.Dispose();

            _logger.Trace($"IoT Hub instance disposed", TraceSeverity.Verbose);
        }
    }
}
