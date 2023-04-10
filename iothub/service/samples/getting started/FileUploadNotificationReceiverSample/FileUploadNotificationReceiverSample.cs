// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Microsoft.Azure.Devices.Samples
{
    /// <summary>
    /// This sample connects to the IoT hub using a connection string and listens on file upload notifications.
    /// After inspecting the notification, the sample will mark it as completed.
    /// The sample will run indefinitely unless specified otherwise using the -r parameter or interrupted by Ctrl+C.
    /// </summary>
    internal class FileUploadNotificationReceiverSample
    {
        private readonly ILogger _logger;
        private static IotHubServiceClient s_serviceClient;
        private static string s_targetDevice;
        private int _totalNotificationsReceived;
        private int _totalNotificationsCompleted;
        private int _totalNotificationsAbandoned;

        public FileUploadNotificationReceiverSample(IotHubServiceClient serviceClient, ILogger logger)
        {
            s_serviceClient = serviceClient ?? throw new ArgumentNullException(nameof(serviceClient));
            _logger = logger;
            _totalNotificationsReceived = 0;
            _totalNotificationsCompleted = 0;
            _totalNotificationsAbandoned = 0;
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
            
            s_targetDevice = targetDeviceId;

            try
            {
                await ReceiveFileUploadNotificationsAsync(targetDeviceId, cts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unrecoverable exception caught, user action is required, exiting: {ex}");
            }
            finally
            {
                _logger.LogInformation($"Closing the service client.");
            }
        }

        private async Task ReceiveFileUploadNotificationsAsync(string targetDeviceId, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(targetDeviceId))
            {
                _logger.LogInformation($"Target device is specified, will only complete matching notifications.");
            }

            _logger.LogInformation($"Listening for file upload notifications from the service.");

            s_serviceClient.FileUploadNotifications.FileUploadNotificationProcessor = FileUploadNotificationCallback;
            s_serviceClient.FileUploadNotifications.ErrorProcessor = OnConnectionLost;
            await s_serviceClient.FileUploadNotifications.OpenAsync(cancellationToken);

            // Wait for cancellation and then print the summary
            try
            {
                await Task.Delay(-1, cancellationToken);
            }
            catch (OperationCanceledException) { }

            _logger.LogInformation($"Total Notifications Received: {_totalNotificationsReceived}.");
            _logger.LogInformation($"Total Notifications Marked as Completed: {_totalNotificationsCompleted}.");
            _logger.LogInformation($"Total Notifications Marked as Abandoned: {_totalNotificationsAbandoned}.");

            await s_serviceClient.FileUploadNotifications.CloseAsync(CancellationToken.None);
        }

        private Task<AcknowledgementType> FileUploadNotificationCallback(FileUploadNotification fileUploadNotification)
        {
            AcknowledgementType ackType;

            _totalNotificationsReceived++;
            var sb = new StringBuilder();
            sb.Append($"Received file upload notification.");
            sb.Append($"\tDeviceId: {fileUploadNotification.DeviceId ?? "N/A"}.");
            sb.Append($"\tFileName: {fileUploadNotification.BlobName ?? "N/A"}.");
            sb.Append($"\tEnqueueTimeUTC: {fileUploadNotification.EnqueuedOnUtc}.");
            sb.Append($"\tBlobSizeInBytes: {fileUploadNotification.BlobSizeInBytes}.");
            _logger.LogInformation(sb.ToString());

            // If the targetDeviceId is set and does not match the notification's origin, ignore it by abandoning the notification.
            // Completing a notification will remove that notification from the service's queue so it won't be delivered to any other receiver again.
            // Abandoning a notification will put it back on the queue to be re-delivered to receivers. This is mostly used when multiple receivers
            // are configured and each receiver is only interested in notifications from a particular device/s.
            if (!string.IsNullOrWhiteSpace(s_targetDevice)
                && !string.Equals(fileUploadNotification.DeviceId, s_targetDevice, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation($"Marking notification for {fileUploadNotification.DeviceId} as Abandoned.");
                ackType = AcknowledgementType.Abandon;
                _totalNotificationsAbandoned++;
            }
            else
            {
                _logger.LogInformation($"Marking notification for {fileUploadNotification.DeviceId} as Completed.");
                ackType = AcknowledgementType.Complete;
                _totalNotificationsCompleted++;
            }

            return Task.FromResult(ackType);
        }

        private async Task OnConnectionLost(FileUploadNotificationProcessorError error)
        {
            _logger.LogError($"Encountered an error while receiving file upload notifications. " +
                $"Error message: {error.Exception.Message}");

            // Note that this client was configured to use retry logic, so this open call will retry even if it fails.
            _logger.LogInformation("Attempting to re-open the connection");
            await s_serviceClient.FileUploadNotifications.OpenAsync();
            _logger.LogInformation("Successfully re-opened the connection");
        }
    }
}
