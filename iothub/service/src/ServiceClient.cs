// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

#if !NET451

using Azure;
using Azure.Core;

#endif

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
    /// Contains methods that services can use to send messages to devices
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
        /// Create ServiceClient from the specified connection string
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns></returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, ServiceClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, TransportType.Amqp, options);
        }

#if !NET451

        /// <summary>
        /// Creates a <see cref="ServiceClient"/> using Azure Active Directory credentials and the specified transport type.
        /// </summary>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Azure Active Directory credentials to authenticate with IoT hub. See <see cref="TokenCredential"/></param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
        /// <param name="transportSettings">Specifies the AMQP_WS and HTTP proxy settings for service client.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of <see cref="ServiceClient"/>.</returns>
        public static ServiceClient Create(
            string hostName,
            TokenCredential credential,
            TransportType transportType,
            ServiceClientTransportSettings transportSettings = default,
            ServiceClientOptions options = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null or empty");
            }

            var tokenCredentialProperties = new IotHubTokenCrendentialProperties(hostName, credential);
            bool useWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only;

            return new AmqpServiceClient(
                tokenCredentialProperties,
                useWebSocketOnly,
                transportSettings ?? new ServiceClientTransportSettings(),
                options);
        }

        /// <summary>
        /// Creates a <see cref="ServiceClient"/> using SAS token and the specified transport type.
        /// </summary>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="IotHubSasCredential"/></param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
        /// <param name="transportSettings">Specifies the AMQP_WS and HTTP proxy settings for service client.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of <see cref="ServiceClient"/>.</returns>
        public static ServiceClient Create(
            string hostName,
            IotHubSasCredential credential,
            TransportType transportType,
            ServiceClientTransportSettings transportSettings = default,
            ServiceClientOptions options = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null or empty");
            }

            var sasCredentialProperties = new IotHubSasCredentialProperties(hostName, credential);
            bool useWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only;

            return new AmqpServiceClient(
                sasCredentialProperties,
                useWebSocketOnly,
                transportSettings ?? new ServiceClientTransportSettings(),
                options);
        }

#endif

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
        /// Create ServiceClient from the specified connection string using specified Transport Type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub</param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns></returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, TransportType transportType, ServiceClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, transportType, new ServiceClientTransportSettings(), options);
        }

        /// <summary>
        /// Create ServiceClient from the specified connection string using specified Transport Type
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub</param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used</param>
        /// <param name="transportSettings">Specifies the AMQP and HTTP proxy settings for Service Client</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns></returns>
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
        /// Open the ServiceClient instance
        /// </summary>
        /// <returns></returns>
        public abstract Task OpenAsync();

        /// <summary>
        /// Close the ServiceClient instance
        /// </summary>
        /// <returns></returns>
        public abstract Task CloseAsync();

        /// <summary>
        /// Send a one-way notification to the specified device
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device</param>
        /// <param name="message">The message containing the notification</param>
        /// <param name="timeout">The operation timeout override. If not used uses OperationTimeout default</param>
        /// <returns></returns>
        public abstract Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null);

        /// <summary>
        /// Removes all messages from a device's queue.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <returns></returns>
        public abstract Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId);

        /// <summary>
        /// Removes all messages from a device's queue.
        /// </summary>
        /// <param name="deviceId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public abstract Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken);

        /// <summary>
        /// Get the FeedbackReceiver
        /// </summary>
        /// <returns>An instance of the FeedbackReceiver</returns>
        public abstract FeedbackReceiver<FeedbackBatch> GetFeedbackReceiver();

        /// <summary>
        /// Get the FeedbackReceiver
        /// </summary>
        /// <returns>An instance of the FeedbackReceiver</returns>
        public abstract FileNotificationReceiver<FileNotification> GetFileNotificationReceiver();

        /// <summary>
        /// Gets service statistics for the Iot Hub.
        /// </summary>
        /// <returns>returns ServiceStatistics object containing current service statistics</returns>
        public abstract Task<ServiceStatistics> GetServiceStatisticsAsync();

        /// <summary>
        /// Gets service statistics for the Iot Hub.
        /// </summary>
        /// <param name="cancellationToken">
        /// The token which allows the the operation to be cancelled.
        /// </param>
        /// <returns>returns ServiceStatistics object containing current service statistics</returns>
        public abstract Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken);

        /// <summary>
        /// Interactively invokes a method on device
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="cloudToDeviceMethod">Device method parameters (passthrough to device)</param>
        /// <returns>Method result</returns>
        public abstract Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod);

        /// <summary>
        /// Interactively invokes a method on device
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="cloudToDeviceMethod">Device method parameters (passthrough to device)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Method result</returns>
        public abstract Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken);

        /// <summary>
        /// Interactively invokes a method on device
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="cloudToDeviceMethod">Device method parameters (passthrough to device)</param>
        /// <returns>Method result</returns>
        public abstract Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod);

        /// <summary>
        /// Interactively invokes a method on device
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="cloudToDeviceMethod">Device method parameters (passthrough to device)</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>Method result</returns>
        public abstract Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken);

        /// <summary>
        /// Send a one-way notification to the specified device module
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device</param>
        /// <param name="moduleId">The module identifier for the target device module</param>
        /// <param name="message">The message containing the notification</param>
        /// <returns></returns>
        public abstract Task SendAsync(string deviceId, string moduleId, Message message);
    }
}
