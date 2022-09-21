// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample connects to the IoT hub using a connection string and listens on file upload notifications.
    /// After inspecting the notification, the sample will mark it as completed.
    /// The sample will run indefinitely unless specified otherwise using the -r parameter or interrupted by Ctrl+C.
    /// </summary>
    public class FileUploadNotificationReceiverSample
    {
        private readonly string _iotHubConnectionString;
        private readonly ILogger _logger;
        private readonly TransportType _transportType;
        private static ServiceClient _serviceClient;

        public FileUploadNotificationReceiverSample(string iotHubConnectionString, TransportType transportType, ILogger logger)
        {
            _iotHubConnectionString = iotHubConnectionString ?? throw new ArgumentNullException(nameof(iotHubConnectionString));
            _transportType = transportType;
            _logger = logger;
        }

        /// <summary>
        /// Listens on file upload notifications on the IoT hub.
        /// </summary>
        /// <param name="targetDeviceId">Device Id used to filter which notifications to complete. Use null to complete all notifications.</param>
        /// <param name="runningTime">Amount of time the method will listen for notifications.</param>
        public async Task RunSampleAsync(string targetDeviceId, TimeSpan runningTime)
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
                await ReceiveFileUploadNotificationsAsync(targetDeviceId, cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, exiting...: \n{ex}");
            }
            finally
            {
                _logger.LogInformation($"Closing the service client.");
                await _serviceClient.CloseAsync();
            }
        }

        private async Task ReceiveFileUploadNotificationsAsync(string targetDeviceId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(targetDeviceId))
            {
                _logger.LogInformation($"Target device is specified, will only complete matching notifications.");
            }

            _logger.LogInformation($"Listening for file upload notifications from the service.");

            FileNotificationReceiver<FileNotification> notificationReceiver = _serviceClient.GetFileNotificationReceiver();

            int totalNotificationsReceived = 0;
            int totalNotificationsCompleted = 0;
            int totalNotificationsAbandoned = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    FileNotification fileUploadNotification = await notificationReceiver.ReceiveAsync(cancellationToken);

                    if (fileUploadNotification != null)
                    {
                        _logger.LogInformation("Did not receive any notification.");
                        continue;
                    }

                    totalNotificationsReceived++;

                    _logger.LogInformation($"Received file upload notification.");
                    _logger.LogInformation($"\tDeviceId: {fileUploadNotification.DeviceId ?? "N/A"}.");
                    _logger.LogInformation($"\tFileName: {fileUploadNotification.BlobName ?? "N/A"}.");
                    _logger.LogInformation($"\tEnqueueTimeUTC: {fileUploadNotification.EnqueuedTimeUtc}.");
                    _logger.LogInformation($"\tBlobSizeInBytes: {fileUploadNotification.BlobSizeInBytes}.");

                    // If the targetDeviceId is set and does not match the notification's origin, ignore it by abandoning the notification.
                    // Completing a notification will remove that notification from the service's queue so it won't be delivered to any other receiver again.
                    // Abandoning a notification will put it back on the queue to be re-delivered to receivers. This is mostly used when multiple receivers
                    // are configured and each receiver is only interested in notifications from a particular device/s.
                    if (!string.IsNullOrWhiteSpace(targetDeviceId)
                        && !string.Equals(fileUploadNotification.DeviceId, targetDeviceId, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.LogInformation($"Marking notification for {fileUploadNotification.DeviceId} as Abandoned.");

                        await notificationReceiver.AbandonAsync(fileUploadNotification, cancellationToken);

                        _logger.LogInformation($"Successfully marked the notification for device {fileUploadNotification.DeviceId} as Abandoned.");
                        totalNotificationsAbandoned++;
                    }
                    else
                    {
                        _logger.LogInformation($"Marking notification for {fileUploadNotification.DeviceId} as Completed.");

                        await notificationReceiver.CompleteAsync(fileUploadNotification, cancellationToken);

                        _logger.LogInformation($"Successfully marked the notification for device {fileUploadNotification.DeviceId} as Completed.");
                        totalNotificationsCompleted++;
                    }

                }
                catch (Exception e) when ((e is IotHubException) || (e is DeviceMessageLockLostException))
                {
                    _logger.LogWarning($"Caught a recoverable exception, will retry: {e.Message} - {e}");
                }
            }

            _logger.LogInformation($"Total Notifications Received: {totalNotificationsReceived}.");
            _logger.LogInformation($"Total Notifications Marked as Completed: {totalNotificationsCompleted}.");
            _logger.LogInformation($"Total Notifications Marked as Abandoned: {totalNotificationsAbandoned}.");
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
                SdkAssignsMessageId = SdkAssignsMessageId.WhenUnset,
            };
            _serviceClient = ServiceClient.CreateFromConnectionString(_iotHubConnectionString, _transportType, options);
            _logger.LogInformation("Initialized a new service client instance.");
        }
    }
}
