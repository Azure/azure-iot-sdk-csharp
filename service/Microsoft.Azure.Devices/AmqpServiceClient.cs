// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
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
    using Microsoft.Azure.Devices.Common.WebApi;

    sealed class AmqpServiceClient : ServiceClient
    {
        static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(100);
        const string StatisticsUriFormat = "/statistics/service?" + ClientApiVersionHelper.ApiVersionQueryString;
        const string PurgeMessageQueueFormat = "/devices/{0}/commands?" + ClientApiVersionHelper.ApiVersionQueryString;
        const string DeviceMethodUriFormat = "/twins/{0}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
#if ENABLE_MODULES_SDK
        const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;
#endif
        readonly IotHubConnection iotHubConnection;
        readonly TimeSpan openTimeout;
        readonly TimeSpan operationTimeout;
        readonly FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantSendingLink;
        readonly string sendingPath;
        readonly string receivingPath;
        readonly AmqpFeedbackReceiver feedbackReceiver;
        readonly AmqpFileNotificationReceiver fileNotificationReceiver;
        readonly IHttpClientHelper httpClientHelper;
        readonly string iotHubName;

        int sendingDeliveryTag;

        public AmqpServiceClient(IotHubConnectionString iotHubConnectionString, bool useWebSocketOnly)
        {
            var iotHubConnection = new IotHubConnection(iotHubConnectionString, AccessRights.ServiceConnect, useWebSocketOnly);
            this.iotHubConnection = iotHubConnection;
            this.openTimeout = IotHubConnection.DefaultOpenTimeout;
            this.operationTimeout = IotHubConnection.DefaultOperationTimeout;
            this.sendingPath = "/messages/deviceBound";
            this.faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateSendingLinkAsync, this.iotHubConnection.CloseLink);
            this.feedbackReceiver = new AmqpFeedbackReceiver(this.iotHubConnection);
            this.fileNotificationReceiver = new AmqpFileNotificationReceiver(this.iotHubConnection);
            this.iotHubName = iotHubConnectionString.IotHubName;
            this.httpClientHelper = new HttpClientHelper(
                iotHubConnectionString.HttpsEndpoint,
                iotHubConnectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                DefaultOperationTimeout,
                client => {});
        }

        internal AmqpServiceClient(IotHubConnectionString iotHubConnectionString, bool useWebSocketOnly, IHttpClientHelper httpClientHelper) : base()
        {
            this.httpClientHelper = httpClientHelper;
        }

        internal AmqpServiceClient(IotHubConnection iotHubConnection, IHttpClientHelper httpClientHelper)
        {
            this.iotHubConnection = iotHubConnection;
            this.faultTolerantSendingLink = new FaultTolerantAmqpObject<SendingAmqpLink>(this.CreateSendingLinkAsync, iotHubConnection.CloseLink);
            this.feedbackReceiver = new AmqpFeedbackReceiver(iotHubConnection);
            this.fileNotificationReceiver = new AmqpFileNotificationReceiver(iotHubConnection);
            this.httpClientHelper = httpClientHelper;
        }

        public TimeSpan OpenTimeout
        {
            get
            {
                return this.openTimeout;
            }
        }

        public TimeSpan OperationTimeout
        {
            get
            {
                return this.operationTimeout;
            }
        }

        public IotHubConnection Connection
        {
            get
            {
                return this.iotHubConnection;
            }
        }

        public SendingAmqpLink SendingLink
        {
            get
            {
                SendingAmqpLink sendingLink;
                this.faultTolerantSendingLink.TryGetOpenedObject(out sendingLink);
                return sendingLink;
            }
        }

        public override async Task OpenAsync()
        {
            await this.GetSendingLinkAsync();
            await this.feedbackReceiver.OpenAsync();
        }

        public async override Task CloseAsync()
        {
            await this.faultTolerantSendingLink.CloseAsync();
            await this.feedbackReceiver.CloseAsync();
            await this.fileNotificationReceiver.CloseAsync();
            await this.iotHubConnection.CloseAsync();
        }

        public async override Task SendAsync(string deviceId, Message message, TimeSpan? timeout = null)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("Value should be non null and non empty", "deviceId");
            }

            if (message == null)
            {
                throw new ArgumentNullException("message");
            }
            Outcome outcome;

            using (AmqpMessage amqpMessage = message.ToAmqpMessage())
            {
                amqpMessage.Properties.To = "/devices/" + WebUtility.UrlEncode(deviceId) + "/messages/deviceBound";
                try
                {
                    SendingAmqpLink sendingLink = await this.GetSendingLinkAsync();
                    if (timeout != null)
                    {
                        outcome = await sendingLink.SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref this.sendingDeliveryTag), AmqpConstants.NullBinary, (TimeSpan)timeout);
                    }
                    else
                    {
                        outcome = await sendingLink.SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref this.sendingDeliveryTag), AmqpConstants.NullBinary, this.OperationTimeout);
                    }
                }
                catch (TimeoutException exception)
                {
                    throw exception;
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

        public override Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId)
        {
            return this.PurgeMessageQueueAsync(deviceId, CancellationToken.None);
        }

        public override Task<PurgeMessageQueueResult> PurgeMessageQueueAsync(string deviceId, CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();
            errorMappingOverrides.Add(HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new DeviceNotFoundException(deviceId)));
            return this.httpClientHelper.DeleteAsync<PurgeMessageQueueResult>(GetPurgeMessageQueueAsyncUri(deviceId), errorMappingOverrides, null, cancellationToken);
        }

        public override FeedbackReceiver<FeedbackBatch> GetFeedbackReceiver()
        {
            return this.feedbackReceiver;
        }

        public override FileNotificationReceiver<FileNotification> GetFileNotificationReceiver()
        {
            return this.fileNotificationReceiver;
        }

        public override Task<ServiceStatistics> GetServiceStatisticsAsync()
        {
            return this.GetServiceStatisticsAsync(CancellationToken.None);
        }

        public override Task<ServiceStatistics> GetServiceStatisticsAsync(CancellationToken cancellationToken)
        {
            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>();
            errorMappingOverrides.Add(HttpStatusCode.NotFound, responseMessage => Task.FromResult((Exception)new IotHubNotFoundException(this.iotHubName)));
            return this.httpClientHelper.GetAsync<ServiceStatistics>(GetStatisticsUri(), errorMappingOverrides, null, cancellationToken);
        }

        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, CloudToDeviceMethod cloudToDeviceMethod)
        {
            return this.InvokeDeviceMethodAsync(deviceId, cloudToDeviceMethod, CancellationToken.None);
        }

        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId,
            CloudToDeviceMethod cloudToDeviceMethod,
            CancellationToken cancellationToken)
        {
            return InvokeDeviceMethodAsync(GetDeviceMethodUri(deviceId), cloudToDeviceMethod, cancellationToken);
        }

        Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(Uri uri,
            CloudToDeviceMethod cloudToDeviceMethod,
            CancellationToken cancellationToken)
        {
            TimeSpan timeout = GetInvokeDeviceMethodOperationTimeout(cloudToDeviceMethod);
            if (!string.IsNullOrEmpty(this.iotHubConnection?.ConnectionString?.ModuleId))
            {
                var customHeaders = new Dictionary<string, string>
                {
                    { CustomHeaderConstants.ModuleId, $"{this.iotHubConnection.ConnectionString.DeviceId}/{this.iotHubConnection.ConnectionString.ModuleId}" }
                };

                return this.httpClientHelper.PostAsync<CloudToDeviceMethod, CloudToDeviceMethodResult>(
                    uri,
                    cloudToDeviceMethod,
                    timeout,
                    null,
                    customHeaders,
                    cancellationToken);
            }
            else
            {
                return this.httpClientHelper.PostAsync<CloudToDeviceMethod, CloudToDeviceMethodResult>(
                    uri,
                    cloudToDeviceMethod,
                    timeout,
                    null,
                    null,
                    cancellationToken);
            }
        }

#if ENABLE_MODULES_SDK
        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod)
        {
            return this.InvokeDeviceMethodAsync(deviceId, moduleId, cloudToDeviceMethod, CancellationToken.None);
        }

        public override Task<CloudToDeviceMethodResult> InvokeDeviceMethodAsync(string deviceId, string moduleId, CloudToDeviceMethod cloudToDeviceMethod, CancellationToken cancellationToken)
        {
            return InvokeDeviceMethodAsync(GetModuleMethodUri(deviceId, moduleId), cloudToDeviceMethod, cancellationToken);
        }

        public override async Task SendAsync(string deviceId, string moduleId, Message message)
        {
            if (string.IsNullOrWhiteSpace(deviceId))
            {
                throw new ArgumentException("Value should be non null and non empty", nameof(deviceId));
            }

            if (string.IsNullOrWhiteSpace(moduleId))
            {
                throw new ArgumentException("Value should be non null and non empty", nameof(moduleId));
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
                    SendingAmqpLink sendingLink = await this.GetSendingLinkAsync();
                    outcome = await sendingLink.SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref this.sendingDeliveryTag), AmqpConstants.NullBinary, this.OperationTimeout);
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
#endif

        async Task<SendingAmqpLink> GetSendingLinkAsync()
        {
            SendingAmqpLink sendingLink;
            if (!this.faultTolerantSendingLink.TryGetOpenedObject(out sendingLink))
            {
                sendingLink = await this.faultTolerantSendingLink.GetOrCreateAsync(this.OpenTimeout);
            }

            return sendingLink;
        }

        Task<SendingAmqpLink> CreateSendingLinkAsync(TimeSpan timeout)
        {
            return this.iotHubConnection.CreateSendingLinkAsync(this.sendingPath, timeout);
        }

        /// <inheritdoc/>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.faultTolerantSendingLink.Dispose();
                this.fileNotificationReceiver.Dispose();
                this.feedbackReceiver.Dispose();
                this.iotHubConnection.Dispose();
                this.httpClientHelper.Dispose();
            }
        }

        static TimeSpan GetInvokeDeviceMethodOperationTimeout(CloudToDeviceMethod cloudToDeviceMethod)
        {
            // For InvokeDeviceMethod, we need to take into account the timeouts specified
            // for the Device to connect and send a response. We also need to take into account
            // the transmission time for the request send/receive
            TimeSpan timeout = TimeSpan.FromSeconds(15); // For wire time
            timeout += TimeSpan.FromSeconds(cloudToDeviceMethod.ConnectionTimeoutInSeconds ?? 0);
            timeout += TimeSpan.FromSeconds(cloudToDeviceMethod.ResponseTimeoutInSeconds ?? 0);
            return timeout <= DefaultOperationTimeout ? DefaultOperationTimeout : timeout;
        }

        static Uri GetStatisticsUri()
        {
            return new Uri(StatisticsUriFormat, UriKind.Relative);
        }

        static Uri GetPurgeMessageQueueAsyncUri(string deviceId)
        {
            return new Uri(PurgeMessageQueueFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceMethodUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

#if ENABLE_MODULES_SDK
        static Uri GetModuleMethodUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(ModuleMethodUriFormat.FormatInvariant(deviceId, moduleId), UriKind.Relative);
        }
#endif
    }
}
