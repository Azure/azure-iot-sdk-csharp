// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Transport types supported by ServiceClient - Amqp and Amqp over WebSocket only
    /// </summary>
#pragma warning disable CA1707 // Identifiers should not contain underscores

    public enum TransportType
    {
        /// <summary>
        /// Advanced Message Queuing Protocol transport.
        /// </summary>
        Amqp,

        /// <summary>
        /// Advanced Message Queuing Protocol transport over WebSocket only.
        /// </summary>
        Amqp_WebSocket_Only
    }

#pragma warning restore CA1707 // Identifiers should not contain underscores

    /// <summary>
    /// Contains methods that services can use to send messages to devices,
    /// invoke a direct method on a device and receive file upload and cloud to device message delivery notifications.
    /// </summary>
    public abstract class ServiceClient : IDisposable
    {
        /// <summary>
        /// Make this constructor internal so that only this library may implement this abstract class.
        /// </summary>
        internal ServiceClient()
        {
            TlsVersions.Instance.SetLegacyAcceptableVersions();
        }

        /// <summary>
        /// Create an instance of ServiceClient from the specified IoT Hub connection string.
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of ServiceClient.</returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, ServiceClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, TransportType.Amqp, options);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing) { }

        /// <summary>
        /// Create an instance of ServiceClient from the specified IoT Hub connection string using specified Transport Type.
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub.</param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of ServiceClient.</returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, TransportType transportType, ServiceClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, transportType, new ServiceClientTransportSettings(), options);
        }

        /// <summary>
        /// Create an instance of ServiceClient from the specified IoT Hub connection string using specified Transport Type and transport settings.
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub.</param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
        /// <param name="transportSettings">Specifies the AMQP and HTTP proxy settings for Service Client.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of ServiceClient.</returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, TransportType transportType, ServiceClientTransportSettings transportSettings, ServiceClientOptions options = default)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            bool useWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only;
            var serviceClient = new AmqpServiceClient(iotHubConnectionString, useWebSocketOnly, transportSettings, options);
            return serviceClient;
        }

        /// <summary>
        /// Open the ServiceClient instance.
        /// </summary>
        public abstract Task OpenAsync();

        /// <summary>
        /// Close the ServiceClient instance.
        /// </summary>
        public abstract Task CloseAsync();

        /// <summary>
        /// Send a cloud to device message to the specified device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="message">The cloud to device message.</param>
        /// <param name="timeout">The operation timeout which defaults to 1 minute, if unspecified.</param>
        public abstract Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null);

        /// <summary>
        /// Removes all cloud to device messages from a device's queue.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        public abstract Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Get the FeedbackReceiver.
        /// </summary>
        /// <returns>An instance of the FeedbackReceiver</returns>
        public abstract FeedbackReceiver<FeedbackBatch> GetFeedbackReceiver();

        /// <summary>
        /// Get the FileNotificationReceiver
        /// </summary>
        /// <returns>An instance of the FileNotificationReceiver</returns>
        public abstract FileNotificationReceiver<FileNotification> GetFileNotificationReceiver();

        /// <summary>
        /// Gets service statistics for the Iot Hub.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The service statistics that can be retrieved from IotHub, eg. the number of device connected to the hub.</returns>
        public abstract Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Interactively invokes a method on device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="cloudToDeviceMethod">Device method parameters (passthrough to the device).</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>Method result.</returns>
        public abstract Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default);

        /// <summary>
        /// Interactively invokes a method on device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="cloudToDeviceMethod">Method parameters (passthrough to the module).</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>Method result.</returns>
        public abstract Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default);

        /// <summary>
        /// Send a cloud to device message to the specified module.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="message">The cloud to module message.</param>
        public abstract Task SendAsync(string deviceId, string moduleId, Message message);
    }
}
