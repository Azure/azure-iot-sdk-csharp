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
using Microsoft.Azure.Devices.Common.Data;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Shared;
using AmqpTrace = Microsoft.Azure.Amqp.AmqpTrace;

namespace Microsoft.Azure.Devices
{
    // This class uses a combination of AMQP and HTTP clients to perform operations.
    internal sealed class AmqpServiceClient : ServiceClient
    {
        private const string StatisticsUriFormat = "/statistics/service?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string PurgeMessageQueueFormat = "/devices/{0}/commands?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string _sendingPath = "/messages/deviceBound";

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);

        private readonly FaultTolerantAmqpObject<SendingAmqpLink> _faultTolerantSendingLink;
        private readonly AmqpFeedbackReceiver _feedbackReceiver;
        private readonly AmqpFileNotificationReceiver _fileNotificationReceiver;
        private readonly IHttpClientHelper _httpClientHelper;
        private readonly string _iotHubName;
        private readonly ServiceClientOptions _clientOptions;

        private int _sendingDeliveryTag;

        public AmqpServiceClient(
            IotHubConnectionProperties connectionProperties,
            bool useWebSocketOnly,
            ServiceClientTransportSettings transportSettings,
            ServiceClientOptions options)
        {
            var iotHubConnection = new IotHubConnection(connectionProperties, AccessRights.ServiceConnect, useWebSocketOnly, transportSettings);
            Connection = iotHubConnection;
            OpenTimeout = IotHubConnection.DefaultOpenTimeout;
            OperationTimeout = IotHubConnection.DefaultOperationTimeout;
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, Connection.CloseLink);
            _feedbackReceiver = new AmqpFeedbackReceiver(Connection);
            _fileNotificationReceiver = new AmqpFileNotificationReceiver(Connection);
            _iotHubName = connectionProperties.IotHubName;
            _clientOptions = options;
            _httpClientHelper = new HttpClientHelper(
                connectionProperties.HttpsEndpoint,
                connectionProperties,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.HttpProxy);

            // Set the trace provider for the AMQP library.
            AmqpTrace.Provider = new AmqpTransportLog();
        }

        internal AmqpServiceClient(IHttpClientHelper httpClientHelper) : base()
        {
            _httpClientHelper = httpClientHelper;
        }

        internal AmqpServiceClient(IotHubConnection iotHubConnection, IHttpClientHelper httpClientHelper)
        {
            Connection = iotHubConnection;
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, iotHubConnection.CloseLink);
            _feedbackReceiver = new AmqpFeedbackReceiver(iotHubConnection);
            _fileNotificationReceiver = new AmqpFileNotificationReceiver(iotHubConnection);
            _httpClientHelper = httpClientHelper;
        }

        public TimeSpan OpenTimeout { get; private set; }

        public TimeSpan OperationTimeout { get; private set; }

        public IotHubConnection Connection { get; private set; }

        // This call is executed over AMQP.
        public override async Task OpenAsync()
        {
            Logging.Enter(this, $"Opening AmqpServiceClient", nameof(OpenAsync));

            await _faultTolerantSendingLink.OpenAsync(OpenTimeout).ConfigureAwait(false);
            await _feedbackReceiver.OpenAsync().ConfigureAwait(false);

            Logging.Exit(this, $"Opening AmqpServiceClient", nameof(OpenAsync));
        }

        // This call is executed over AMQP.
        public async override Task CloseAsync()
        {
            Logging.Enter(this, $"Closing AmqpServiceClient", nameof(CloseAsync));

            await _faultTolerantSendingLink.CloseAsync().ConfigureAwait(false);
            await _feedbackReceiver.CloseAsync().ConfigureAwait(false);
            await _fileNotificationReceiver.CloseAsync().ConfigureAwait(false);
            await Connection.CloseAsync().ConfigureAwait(false);

            Logging.Exit(this, $"Closing AmqpServiceClient", nameof(CloseAsync));
        }

        // This call is executed over AMQP.
        public async override Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
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

            timeout ??= OperationTimeout;

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

        // This call is executed over HTTP.
        public override Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId)
        {
            return PurgeMessageQueueAsync(deviceId, CancellationToken.None);
        }

        // This call is executed over HTTP.
        public override Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken)
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

        // This call is executed over AMQP.
        public override FeedbackReceiver<FeedbackBatch> GetFeedbackReceiver()
        {
            return _feedbackReceiver;
        }

        // This call is executed over AMQP.
        public override FileNotificationReceiver<FileNotification> GetFileNotificationReceiver()
        {
            return _fileNotificationReceiver;
        }

        // This call is executed over HTTP.
        public override Task<ServiceStatistics> GetServiceStatisticsAsync()
        {
            return GetServiceStatisticsAsync(CancellationToken.None);
        }

        // This call is executed over HTTP.
        public override Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken)
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

        // This call is executed over HTTP.
        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod)
        {
            return InvokeDeviceMethodAsync(deviceId, cloudToDeviceMethod, CancellationToken.None);
        }

        // This call is executed over HTTP.
        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId,
            CloudToDeviceMethod cloudToDeviceMethod,
            CancellationToken cancellationToken)
        {
            return InvokeDeviceMethodAsync(GetDeviceMethodUri(deviceId), cloudToDeviceMethod, cancellationToken);
        }

        // This call is executed over HTTP.
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

        // This call is executed over HTTP.
        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod)
        {
            return InvokeDeviceMethodAsync(deviceId, moduleId, cloudToDeviceMethod, CancellationToken.None);
        }

        // This call is executed over HTTP.
        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken)
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

        // This call is executed over AMQP.
        public override async Task SendAsync(string deviceId, string moduleId, Message message)
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
                        OperationTimeout)
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

        private async Task<SendingAmqpLink> GetSendingLinkAsync()
        {
            Logging.Enter(this, $"_faultTolerantSendingLink = {_faultTolerantSendingLink?.GetHashCode()}", nameof(GetSendingLinkAsync));

            try
            {
                if (!_faultTolerantSendingLink.TryGetOpenedObject(out SendingAmqpLink sendingLink))
                {
                    sendingLink = await _faultTolerantSendingLink.GetOrCreateAsync(OpenTimeout).ConfigureAwait(false);
                }

                Logging.Info(this, $"Retrieved SendingAmqpLink [{sendingLink?.Name}]", nameof(GetSendingLinkAsync));

                return sendingLink;
            }
            finally
            {
                Logging.Exit(this, $"_faultTolerantSendingLink = {_faultTolerantSendingLink?.GetHashCode()}", nameof(GetSendingLinkAsync));
            }
        }

        private Task<SendingAmqpLink> CreateSendingLinkAsync(TimeSpan timeout)
        {
            return Connection.CreateSendingLinkAsync(_sendingPath, timeout);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing)
            {
                _faultTolerantSendingLink.Dispose();
                _fileNotificationReceiver.Dispose();
                _feedbackReceiver.Dispose();
                Connection.Dispose();
                _httpClientHelper.Dispose();
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
