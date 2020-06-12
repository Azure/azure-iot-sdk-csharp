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

namespace Microsoft.Azure.Devices
{
    internal sealed class AmqpServiceClient : ServiceClient
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

        private int _sendingDeliveryTag;

        public AmqpServiceClient(IotHubConnectionString iotHubConnectionString, bool useWebSocketOnly, ServiceClientTransportSettings transportSettings)
        {
            var iotHubConnection = new IotHubConnection(iotHubConnectionString, AccessRights.ServiceConnect, useWebSocketOnly, transportSettings);
            Connection = iotHubConnection;
            OpenTimeout = IotHubConnection.DefaultOpenTimeout;
            OperationTimeout = IotHubConnection.DefaultOperationTimeout;
            _sendingPath = "/messages/deviceBound";
            _faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(CreateSendingLinkAsync, Connection.CloseLink);
            _feedbackReceiver = new AmqpFeedbackReceiver(Connection);
            _fileNotificationReceiver = new AmqpFileNotificationReceiver(Connection);
            _iotHubName = iotHubConnectionString.IotHubName;
            _httpClientHelper = new HttpClientHelper(
                iotHubConnectionString.HttpsEndpoint,
                iotHubConnectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.HttpProxy);
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

        public SendingAmqpLink SendingLink
        {
            get
            {
                _faultTolerantSendingLink.TryGetOpenedObject(out SendingAmqpLink sendingLink);
                return sendingLink;
            }
        }

        public override async Task OpenAsync()
        {
            await GetSendingLinkAsync().ConfigureAwait(false);
            await _feedbackReceiver.OpenAsync().ConfigureAwait(false);
        }

        public async override Task CloseAsync()
        {
            await _faultTolerantSendingLink.CloseAsync().ConfigureAwait(false);
            await _feedbackReceiver.CloseAsync().ConfigureAwait(false);
            await _fileNotificationReceiver.CloseAsync().ConfigureAwait(false);
            await Connection.CloseAsync().ConfigureAwait(false);
        }

        public async override Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentNullException(nameof(deviceId));
            }

            if (message == null)
            {
                throw new ArgumentNullException(nameof(message));
            }
            Outcome outcome;

            using (AmqpMessage amqpMessage = message.ToAmqpMessage())
            {
                amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/messages/deviceBound";
                try
                {
                    SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                    if (timeout != null)
                    {
                        outcome = await sendingLink
                            .SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag), AmqpConstants.NullBinary, (TimeSpan)timeout)
                            .ConfigureAwait(false);
                    }
                    else
                    {
                        outcome = await sendingLink
                            .SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag), AmqpConstants.NullBinary, OperationTimeout)
                            .ConfigureAwait(false);
                    }
                }
                catch (TimeoutException)
                {
                    throw;
                }
                catch (Exception ex) when (!ex.IsFatal())
                {
                    throw AmqpClientHelper.ToIotHubClientContract(ex);
                }
            }
            if (outcome.DescriptorCode != Accepted.Code)
            {
                throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
            }
        }

        public override Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId)
        {
            return PurgeMessageQueueAsync(deviceId, CancellationToken.None);
        }

        public override Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();
            errorMappingOverrides.Add(HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new DeviceNotFoundException(deviceId)));
            return _httpClientHelper.DeleteAsync<PurgeMessageQueueResult>(GetPurgeMessageQueueAsyncUri(deviceId), errorMappingOverrides, null, cancellationToken);
        }

        public override FeedbackReceiver<FeedbackBatch> GetFeedbackReceiver()
        {
            return _feedbackReceiver;
        }

        public override FileNotificationReceiver<FileNotification> GetFileNotificationReceiver()
        {
            return _fileNotificationReceiver;
        }

        public override Task<ServiceStatistics> GetServiceStatisticsAsync()
        {
            return GetServiceStatisticsAsync(CancellationToken.None);
        }

        public override Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();
            errorMappingOverrides.Add(HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new IotHubNotFoundException(_iotHubName)));
            return _httpClientHelper.GetAsync<ServiceStatistics>(GetStatisticsUri(), errorMappingOverrides, null, cancellationToken);
        }

        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod)
        {
            return InvokeDeviceMethodAsync(deviceId, cloudToDeviceMethod, CancellationToken.None);
        }

        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId,
            CloudToDeviceMethod cloudToDeviceMethod,
            CancellationToken cancellationToken)
        {
            return InvokeDeviceMethodAsync(GetDeviceMethodUri(deviceId), cloudToDeviceMethod, cancellationToken);
        }

        private Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(Uri uri,
            CloudToDeviceMethod cloudToDeviceMethod,
            CancellationToken cancellationToken)
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

        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod)
        {
            return InvokeDeviceMethodAsync(deviceId, moduleId, cloudToDeviceMethod, CancellationToken.None);
        }

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

        public override async Task SendAsync(string deviceId, string moduleId, Message message)
        {
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

            Outcome outcome;
            using (AmqpMessage amqpMessage = message.ToAmqpMessage())
            {
                amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/modules/" + WebUtility.UrlEncode(moduleId) + "/messages/deviceBound";
                try
                {
                    SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                    outcome = await sendingLink.SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref _sendingDeliveryTag), AmqpConstants.NullBinary, OperationTimeout).ConfigureAwait(false);
                }
                catch (Exception exception)
                {
                    if (exception.IsFatal())
                    {
                        throw;
                    }

                    throw AmqpClientHelper.ToIotHubClientContract(exception);
                }
            }

            if (outcome.DescriptorCode != Accepted.Code)
            {
                throw AmqpErrorMapper.GetExceptionFromOutcome(outcome);
            }
        }

        private async Task<SendingAmqpLink> GetSendingLinkAsync()
        {
            if (!_faultTolerantSendingLink.TryGetOpenedObject(out SendingAmqpLink sendingLink))
            {
                sendingLink = await _faultTolerantSendingLink.GetOrCreateAsync(OpenTimeout).ConfigureAwait(false);
            }

            return sendingLink;
        }

        private Task<SendingAmqpLink> CreateSendingLinkAsync(TimeSpan timeout)
        {
            return Connection.CreateSendingLinkAsync(_sendingPath, timeout);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
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
