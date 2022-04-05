// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Framing;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using Azure;
using Azure.Core;

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
    /// <remarks>
    /// For more information, see <see href="https://github.com/Azure/azure-iot-sdk-csharp#iot-hub-service-sdk"/>
    /// </remarks>
    public class ServiceClient : IDisposable
    {
        private const string StatisticsUriFormat = "/statistics/service?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string PurgeMessageQueueFormat = "/devices/{0}/commands?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        private readonly FaultTolerantAmqpObject<SendingAmqpLink> _faultTolerantSendingLink;
        private readonly string _sendingPath;
        private readonly AmqpFeedbackReceiver _feedbackReceiver;
        private readonly AmqpFileNotificationReceiver _fileNotificationReceiver;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly string _iotHubName;
        private readonly ServiceClientOptions _clientOptions;
        private readonly TimeSpan _openTimeout;
        private readonly TimeSpan _operationTimeout;

        private int _sendingDeliveryTag;

        /// <summary>
        /// Creates an instance of <see cref="ServiceClient"/>, provided for unit testing purposes only.
        /// Use the CreateFromConnectionString method to create an instance to use the client.
        /// </summary>
        public ServiceClient()
        {
        }

        internal ServiceClient(
            IotHubConnectionProperties connectionProperties,
            bool useWebSocketOnly,
            ServiceClientTransportSettings transportSettings,
            ServiceClientOptions options)
        {
            Connection = new IotHubConnection(connectionProperties, useWebSocketOnly, transportSettings); ;
            _openTimeout = IotHubConnection.DefaultOpenTimeout;
            _operationTimeout = IotHubConnection.DefaultOperationTimeout;
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, Connection.CloseLink);
            _feedbackReceiver = new AmqpFeedbackReceiver(Connection);
            _fileNotificationReceiver = new AmqpFileNotificationReceiver(Connection);
            _iotHubName = connectionProperties.IotHubName;
            _clientOptions = options;
            _sendingPath = "/messages/deviceBound";
            _httpClientHelper = new HttpClientHelper(
                connectionProperties.HttpsEndpoint,
                connectionProperties,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.HttpProxy,
                transportSettings.ConnectionLeaseTimeoutMilliseconds);

            // Set the trace provider for the AMQP library.
            AmqpTrace.Provider = new AmqpTransportLog();
        }

        // internal test helper
        internal ServiceClient(IotHubConnection connection, IHttpClientHelper httpClientHelper)
        {
            Connection = connection;
            _httpClientHelper = httpClientHelper;
            _feedbackReceiver = new AmqpFeedbackReceiver(Connection);
            _fileNotificationReceiver = new AmqpFileNotificationReceiver(Connection);
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, Connection.CloseLink);
        }

        /// <summary>
        /// Creates ServiceClient from an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT hub.</param>
        /// <param name="options">The <see cref="ServiceClientOptions"/> that allow configuration of the service client instance during initialization.</param>
        /// <returns>A ServiceClient instance.</returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, ServiceClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, TransportType.Amqp, options);
        }

        /// <summary>
        /// Creates ServiceClient, authenticating using an identity in Azure Active Directory (AAD).
        /// </summary>
        /// <remarks>
        /// For more about information on the options of authenticating using a derived instance of <see cref="TokenCredential"/>, see
        /// <see href="https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme"/>.
        /// For more information on configuring IoT hub with Azure Active Directory, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Azure Active Directory credentials to authenticate with IoT hub. See <see cref="TokenCredential"/></param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
        /// <param name="transportSettings">Specifies the AMQP_WS and HTTP proxy settings for service client.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>A ServiceClient instance.</returns>
        public static ServiceClient Create(
            string hostName,
            TokenCredential credential,
            TransportType transportType = TransportType.Amqp,
            ServiceClientTransportSettings transportSettings = default,
            ServiceClientOptions options = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)}, Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var tokenCredentialProperties = new IotHubTokenCrendentialProperties(hostName, credential);
            bool useWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only;

            return new ServiceClient(
                tokenCredentialProperties,
                useWebSocketOnly,
                transportSettings ?? new ServiceClientTransportSettings(),
                options);
        }

        /// <summary>
        /// Creates ServiceClient using a shared access signature provided and refreshed as necessary by the caller.
        /// </summary>
        /// <remarks>
        /// Users may wish to build their own shared access signature (SAS) tokens rather than give the shared key to the SDK and let it manage signing and renewal.
        /// The <see cref="AzureSasCredential"/> object gives the SDK access to the SAS token, while the caller can update it as necessary using the
        /// <see cref="AzureSasCredential.Update(string)"/> method.
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="transportType">Specifies whether Amqp or Amqp_WebSocket_Only transport is used.</param>
        /// <param name="transportSettings">Specifies the AMQP_WS and HTTP proxy settings for service client.</param>
        /// <param name="options">The options that allow configuration of the service client instance during initialization.</param>
        /// <returns>A ServiceClient instance.</returns>
        public static ServiceClient Create(
            string hostName,
            AzureSasCredential credential,
            TransportType transportType = TransportType.Amqp,
            ServiceClientTransportSettings transportSettings = default,
            ServiceClientOptions options = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var sasCredentialProperties = new IotHubSasCredentialProperties(hostName, credential);
            bool useWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only;

            return new ServiceClient(
                sasCredentialProperties,
                useWebSocketOnly,
                transportSettings ?? new ServiceClientTransportSettings(),
                options);
        }

        internal IotHubConnection Connection { get; }

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
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _faultTolerantSendingLink.Dispose();
                _fileNotificationReceiver.Dispose();
                _feedbackReceiver.Dispose();
                Connection.Dispose();
                _httpClientHelper.Dispose();
            }
        }

        /// <summary>
        /// Create an instance of ServiceClient from the specified IoT Hub connection string using specified Transport Type.
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub.</param>
        /// <param name="transportType">The <see cref="TransportType"/> used (Amqp or Amqp_WebSocket_Only).</param>
        /// <param name="options">The <see cref="ServiceClientOptions"/> that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of ServiceClient.</returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, TransportType transportType, ServiceClientOptions options = default)
        {
            return CreateFromConnectionString(connectionString, transportType, new ServiceClientTransportSettings(), options);
        }

        /// <summary>
        /// Create an instance of ServiceClient from the specified IoT Hub connection string using specified Transport Type and transport settings.
        /// </summary>
        /// <param name="connectionString">Connection string for the IoT Hub.</param>
        /// <param name="transportType">The <see cref="TransportType"/> used (Amqp or Amqp_WebSocket_Only).</param>
        /// <param name="transportSettings">Specifies the AMQP and HTTP proxy settings for Service Client.</param>
        /// <param name="options">The <see cref="ServiceClientOptions"/> that allow configuration of the service client instance during initialization.</param>
        /// <returns>An instance of ServiceClient.</returns>
        public static ServiceClient CreateFromConnectionString(string connectionString, TransportType transportType, ServiceClientTransportSettings transportSettings, ServiceClientOptions options = default)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings));
            }

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            bool useWebSocketOnly = transportType == TransportType.Amqp_WebSocket_Only;

            return new ServiceClient(
                iotHubConnectionString,
                useWebSocketOnly,
                transportSettings,
                options);
        }

        /// <summary>
        /// Open the ServiceClient instance. This call is made over AMQP.
        /// </summary>
        public virtual async Task OpenAsync()
        {
            Logging.Enter(this, $"Opening AmqpServiceClient", nameof(OpenAsync));

            using var ctx = new CancellationTokenSource(_openTimeout);

            await _faultTolerantSendingLink.OpenAsync(ctx.Token).ConfigureAwait(false);
            await _feedbackReceiver.OpenAsync().ConfigureAwait(false);

            Logging.Exit(this, $"Opening AmqpServiceClient", nameof(OpenAsync));
        }

        /// <summary>
        /// Close the ServiceClient instance. This call is made over AMQP.
        /// </summary>
        public virtual async Task CloseAsync()
        {
            Logging.Enter(this, $"Closing AmqpServiceClient", nameof(CloseAsync));

            await _faultTolerantSendingLink.CloseAsync().ConfigureAwait(false);
            await _feedbackReceiver.CloseAsync().ConfigureAwait(false);
            await _fileNotificationReceiver.CloseAsync().ConfigureAwait(false);
            await Connection.CloseAsync().ConfigureAwait(false);

            Logging.Exit(this, $"Closing AmqpServiceClient", nameof(CloseAsync));
        }

        /// <summary>
        /// Send a cloud-to-device message to the specified device. This call is made over AMQP.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="message">The cloud-to-device message.</param>
        /// <param name="timeout">The operation timeout, which defaults to 1 minute if unspecified.</param>
        public virtual async Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
        {
            Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}", nameof(SendAsync));

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            if (message.IsBodyCalled)
            {
                message.ResetBody();
            }

            timeout ??= _operationTimeout;

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/messages/deviceBound";

            try
            {
                SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                Outcome outcome = await sendingLink
                    .SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag), AmqpConstants.NullBinary, timeout.Value)
                    .ConfigureAwait(false);

                Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (!(ex is TimeoutException) && !ex.IsFatal())
            {
                Logging.Error(this, $"{nameof(SendAsync)} threw an exception: {ex}", nameof(SendAsync));
                throw AmqpClientHelper.ToIotHubClientContract(ex);
            }
            finally
            {
                Logging.Exit(this, $"Sending message [{message?.MessageId}] for device {deviceId}", nameof(SendAsync));
            }
        }

        /// <summary>
        /// Removes all cloud-to-device messages from a device's queue. This call is made over HTTP.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        public virtual Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Purging message queue for device: {deviceId}", nameof(PurgeMessageQueueAsync));

            try
            {
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new DeviceNotFoundException(deviceId)) }
                };

                return _httpClientHelper.DeleteAsync<PurgeMessageQueueResult>(GetPurgeMessageQueueAsyncUri(deviceId), errorMappingOverrides, null, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(PurgeMessageQueueAsync)} threw an exception: {ex}", nameof(PurgeMessageQueueAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Purging message queue for device: {deviceId}", nameof(PurgeMessageQueueAsync));
            }
        }

        /// <summary>
        /// Get the <see cref="FeedbackReceiver{FeedbackBatch}"/> which can deliver acknowledgments for messages sent to a device/module from IoT Hub.
        /// This call is made over AMQP.
        /// For more information see <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d#message-feedback"/>.
        /// </summary>
        /// <returns>An instance of <see cref="FeedbackReceiver{FeedbackBatch}"/>.</returns>
        public virtual FeedbackReceiver<FeedbackBatch> GetFeedbackReceiver()
        {
            return _feedbackReceiver;
        }

        /// <summary>
        /// Get the <see cref="FileNotificationReceiver{FileNotification}"/> which can deliver notifications for file upload operations.
        /// This call is made over AMQP.
        /// For more information see <see href = "https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#file-upload-notifications"/>.
        /// </summary>
        /// <returns>An instance of <see cref="FileNotificationReceiver{FileNotification}"/>.</returns>
        public virtual FileNotificationReceiver<FileNotification> GetFileNotificationReceiver()
        {
            return _fileNotificationReceiver;
        }

        /// <summary>
        /// Gets service statistics for the IoT Hub. This call is made over HTTP.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The service statistics that can be retrieved from IoT Hub, eg. the number of devices connected to the hub.</returns>
        public virtual Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken = default)
        {
            Logging.Enter(this, $"Getting service statistics", nameof(GetServiceStatisticsAsync));

            try
            {
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new IotHubNotFoundException(_iotHubName)) }
                };

                return _httpClientHelper.GetAsync<ServiceStatistics>(GetStatisticsUri(), errorMappingOverrides, null, cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(GetServiceStatisticsAsync)} threw an exception: {ex}", nameof(GetServiceStatisticsAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Getting service statistics", nameof(GetServiceStatisticsAsync));
            }
        }

        /// <summary>
        /// Interactively invokes a method on a device.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="cloudToDeviceMethod">Parameters to execute a direct method on the device.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="CloudToDeviceMethodResult"/>.</returns>
        public virtual Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default)
        {
            return InvokeDeviceMethodAsync(GetDeviceMethodUri(deviceId), cloudToDeviceMethod, cancellationToken);
        }

        /// <summary>
        /// Interactively invokes a method on a module.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="cloudToDeviceMethod">Parameters to execute a direct method on the module.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The <see cref="CloudToDeviceMethodResult"/>.</returns>
        public virtual Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new ArgumentNullException(nameof(moduleId));
            }

            return InvokeDeviceMethodAsync(GetModuleMethodUri(deviceId, moduleId), cloudToDeviceMethod, cancellationToken);
        }

        /// <summary>
        /// Send a cloud-to-device message to the specified module.
        /// </summary>
        /// <param name="deviceId">The device identifier for the target device.</param>
        /// <param name="moduleId">The module identifier for the target module.</param>
        /// <param name="message">The cloud-to-module message.</param>
        ///  <param name="timeout">The operation timeout, which defaults to 1 minute if unspecified.</param>
        public virtual async Task SendAsync(string deviceId, string moduleId, Message message, TimeSpan? timeout = null)
        {
            Logging.Enter(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId}", nameof(SendAsync));

            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new ArgumentNullException(nameof(moduleId));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }

            if (_clientOptions?.SdkAssignsMessageId == SdkAssignsMessageId.WhenUnset && message.MessageId == null)
            {
                message.MessageId = Guid.NewGuid().ToString();
            }

            if (message.IsBodyCalled)
            {
                message.ResetBody();
            }

            timeout ??= _operationTimeout;

            using AmqpMessage amqpMessage = MessageConverter.MessageToAmqpMessage(message);
            amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/modules/" + WebUtility.UrlEncode(moduleId) + "/messages/deviceBound";
            try
            {
                SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                Outcome outcome = await sendingLink
                    .SendMessageAsync(
                        amqpMessage,
                        IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag),
                        AmqpConstants.NullBinary,
                        timeout.Value)
                    .ConfigureAwait(false);

                Logging.Info(this, $"Outcome was: {outcome?.DescriptorName}", nameof(SendAsync));

                if (outcome.DescriptorCode != Accepted.Code)
                {
                    throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
                }
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                Logging.Error(this, $"{nameof(SendAsync)} threw an exception: {ex}", nameof(SendAsync));
                throw AmqpClientHelper.ToIotHubClientContract(ex);
            }
            finally
            {
                Logging.Exit(this, $"Sending message with Id [{message?.MessageId}] for device {deviceId}, module {moduleId}", nameof(SendAsync));
            }
        }

        private Task<SendingAmqpLink> CreateSendingLinkAsync(TimeSpan timeout)
        {
            return Connection.CreateSendingLinkAsync(_sendingPath, timeout);
        }

        private async Task<SendingAmqpLink> GetSendingLinkAsync()
        {
            Logging.Enter(this, $"_faultTolerantSendingLink = {_faultTolerantSendingLink?.GetHashCode()}", nameof(GetSendingLinkAsync));

            try
            {
                if (!_faultTolerantSendingLink.TryGetOpenedObject(out SendingAmqpLink sendingLink))
                {
                    sendingLink = await _faultTolerantSendingLink.GetOrCreateAsync(_openTimeout).ConfigureAwait(false);
                }

                Logging.Info(this, $"Retrieved SendingAmqpLink [{sendingLink?.Name}]", nameof(GetSendingLinkAsync));

                return sendingLink;
            }
            finally
            {
                Logging.Exit(this, $"_faultTolerantSendingLink = {_faultTolerantSendingLink?.GetHashCode()}", nameof(GetSendingLinkAsync));
            }
        }

        private Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(Uri uri,
            CloudToDeviceMethod cloudToDeviceMethod,
            CancellationToken cancellationToken)
        {
            Logging.Enter(this, $"Invoking device method for: {uri}", nameof(InvokeDeviceMethodAsync));

            try
            {
                TimeSpan timeout = GetInvokeDeviceMethodOperationTimeout(cloudToDeviceMethod);

                return _httpClientHelper.PostAsync<CloudToDeviceMethod, CloudToDeviceMethodResult>(
                    uri,
                    cloudToDeviceMethod,
                    timeout,
                    null,
                    null,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(InvokeDeviceMethodAsync)} threw an exception: {ex}", nameof(InvokeDeviceMethodAsync));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Invoking device method for: {uri}", nameof(InvokeDeviceMethodAsync));
            }
        }

        private static TimeSpan GetInvokeDeviceMethodOperationTimeout(CloudToDeviceMethod cloudToDeviceMethod)
        {
            // For InvokeDeviceMethod, we need to take into account the timeouts specified
            // for the Device to connect and send a response. We also need to take into account
            // the transmission time for the request send/receive
            var timeout = TimeSpan.FromSeconds(15); // For wire time
            timeout += TimeSpan.FromSeconds(cloudToDeviceMethod.ConnectionTimeoutInSeconds ?? 0);
            timeout += TimeSpan.FromSeconds(cloudToDeviceMethod.ResponseTimeoutInSeconds ?? 0);
            return timeout <= s_defaultOperationTimeout ? s_defaultOperationTimeout : timeout;
        }

        private static Uri GetStatisticsUri()
        {
            return new Uri(StatisticsUriFormat, UriKind.Relative);
        }

        private static Uri GetPurgeMessageQueueAsyncUri(string deviceId)
        {
            return new Uri(PurgeMessageQueueFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceMethodUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        private static Uri GetModuleMethodUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModuleMethodUriFormat.FormatInvariant(deviceId, moduleId), UriKind.Relative);
        }
    }
}
