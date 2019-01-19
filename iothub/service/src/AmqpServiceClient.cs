// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
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
        const string DeviceStreamUriFormat = "/twins/{0}/streams/{1}?" + ClientApiVersionHelper.ApiVersionQueryString;
        const string ModuleStreamUriFormat = "/twins/{0}/modules/{1}/streams/{2}?" + ClientApiVersionHelper.ApiVersionQueryString;
        const string ModuleMethodUriFormat = "/twins/{0}/modules/{1}/methods?" + ClientApiVersionHelper.ApiVersionQueryString;

        readonly IotHubConnection iotHubConnection;
        readonly TimeSpan openTimeout;
        readonly TimeSpan operationTimeout;
        readonly FaultTolerantAmqpObject<SendingAmqpLink> faultTolerantSendingLink;
        readonly string sendingPath;
        readonly AmqpFeedbackReceiver feedbackReceiver;
        readonly AmqpFileNotificationReceiver fileNotificationReceiver;
        readonly IHttpClientHelper httpClientHelper;
        readonly string iotHubName;

        int sendingDeliveryTag;

        public AmqpServiceClient(IotHubConnectionString iotHubConnectionString, bool useWebSocketOnly, ServiceClientTransportSettings transportSettings)
        {
            var iotHubConnection = new IotHubConnection(iotHubConnectionString, AccessRights.ServiceConnect, useWebSocketOnly, transportSettings);
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
                client => { },
                transportSettings.HttpProxy);
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
            await GetSendingLinkAsync().ConfigureAwait(false);
            await feedbackReceiver.OpenAsync().ConfigureAwait(false);
        }

        public async override Task CloseAsync()
        {
            await faultTolerantSendingLink.CloseAsync().ConfigureAwait(false);
            await feedbackReceiver.CloseAsync().ConfigureAwait(false);
            await fileNotificationReceiver.CloseAsync().ConfigureAwait(false);
            await iotHubConnection.CloseAsync().ConfigureAwait(false);
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
                    SendingAmqpLink sendingLink = await GetSendingLinkAsync().ConfigureAwait(false);
                    if (timeout != null)
                    {
                        outcome = await sendingLink.SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref sendingDeliveryTag), AmqpConstants.NullBinary, (TimeSpan)timeout).ConfigureAwait(false);
                    }
                    else
                    {
                        outcome = await sendingLink.SendMessageAsync(amqpMessage, IotHubConnection.GetNextDeliveryTag(ref sendingDeliveryTag), AmqpConstants.NullBinary, OperationTimeout).ConfigureAwait(false);
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

            return this.httpClientHelper.PostAsync<CloudToDeviceMethod, CloudToDeviceMethodResult>(
                uri,
                cloudToDeviceMethod,
                timeout,
                null,
                null,
                cancellationToken);

        }

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
		
        /// <summary>
        /// Initiates a new cloud-to-device stream.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="deviceStreamRequest">Configuration needed for initiating a cloud-to-device stream.</param>
        /// <returns>The result of the cloud-to-device stream request, or null of the request itself could not be completed.</returns>
        public override Task<DeviceStreamResponse> CreateStreamAsync(string deviceId, DeviceStreamRequest deviceStreamRequest)
        {
            return this.CreateStreamAsync(deviceId, deviceStreamRequest, CancellationToken.None);
        }

        /// <summary>
        /// Initiates a new cloud-to-device stream.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="deviceStreamRequest">Configuration needed for initiating a cloud-to-device stream.</param>
        /// <param name="cancellationToken">Token used for controlling the termination of the asynchronous call.</param>
        /// <returns>The result of the cloud-to-device stream request, or null of the request itself could not be completed.</returns>
        public override Task<DeviceStreamResponse> CreateStreamAsync(string deviceId, DeviceStreamRequest deviceStreamRequest, CancellationToken cancellationToken)
        {
            return this.CreateStreamAsync(GetDeviceStreamUri(deviceId, deviceStreamRequest.StreamName), deviceStreamRequest, cancellationToken);
        }

        /// <summary>
        /// Initiates a new cloud-to-device stream.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module ID</param>
        /// <param name="deviceStreamRequest">Configuration needed for initiating a cloud-to-device stream.</param>
        /// <returns>The result of the cloud-to-device stream request, or null of the request itself could not be completed.</returns>
        public override Task<DeviceStreamResponse> CreateStreamAsync(string deviceId, string moduleId, DeviceStreamRequest deviceStreamRequest)
        {
            return this.CreateStreamAsync(GetModuleStreamUri(deviceId, moduleId, deviceStreamRequest.StreamName), deviceStreamRequest, CancellationToken.None);
        }

        /// <summary>
        /// Initiates a new cloud-to-device stream.
        /// </summary>
        /// <param name="deviceId">Device Id</param>
        /// <param name="moduleId">Module Id</param>
        /// <param name="deviceStreamRequest">Configuration needed for initiating a cloud-to-device stream.</param>
        /// <param name="cancellationToken">Token used for controlling the termination of the asynchronous call.</param>
        /// <returns>The result of the cloud-to-device stream request, or null of the request itself could not be completed.</returns>
        public override Task<DeviceStreamResponse> CreateStreamAsync(string deviceId, string moduleId, DeviceStreamRequest deviceStreamRequest, CancellationToken cancellationToken)
        {
            return this.CreateStreamAsync(GetModuleStreamUri(deviceId, moduleId, deviceStreamRequest.StreamName), deviceStreamRequest, cancellationToken);
        }

        internal async Task<DeviceStreamResponse> CreateStreamAsync(
            Uri uri,
            DeviceStreamRequest deviceStreamRequest,
            CancellationToken cancellationToken)
        {
            DeviceStreamResponse result;

            TimeSpan timeout = GetInitiateStreamOperationTimeout(deviceStreamRequest);

            var customHeaders = new Dictionary<string, string>();

            if (deviceStreamRequest.ConnectionTimeout > TimeSpan.Zero)
            {
                customHeaders["iothub-streaming-connect-timeout-in-seconds"] = deviceStreamRequest.ConnectionTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }

            if (deviceStreamRequest.ResponseTimeout > TimeSpan.Zero)
            {
                customHeaders["iothub-streaming-response-timeout-in-seconds"] = deviceStreamRequest.ResponseTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture);
            }

            var httpResponse = await this.httpClientHelper.PostAsync<byte[], HttpResponseMessage>(
                 uri,
                 null as byte[],
                 timeout,
                 null,
                 customHeaders,
                 cancellationToken).ConfigureAwait(false);

            if (httpResponse.StatusCode != HttpStatusCode.OK &&
                httpResponse.StatusCode != HttpStatusCode.Accepted)
            {
                // Log error when we have a solution for logging.
                result = null;
            }
            else
            {
                bool isAccepted = bool.Parse(httpResponse.Headers.GetValues("iothub-streaming-is-accepted").Single());
                string proxyUri = null;
                string authToken = null;

                if (isAccepted)
                {
                    proxyUri = httpResponse.Headers.GetValues("iothub-streaming-url").Single();
                    authToken = httpResponse.Headers.GetValues("iothub-streaming-auth-token").Single();
                }

                result = new DeviceStreamResponse(
                    deviceStreamRequest.StreamName,
                    isAccepted,
                    authToken,
                    proxyUri != null ? new Uri(proxyUri) : null
                );
            }

            return result;
        }

        async Task<SendingAmqpLink> GetSendingLinkAsync()
        {
            SendingAmqpLink sendingLink;
            if (!this.faultTolerantSendingLink.TryGetOpenedObject(out sendingLink))
            {
                sendingLink = await faultTolerantSendingLink.GetOrCreateAsync(OpenTimeout).ConfigureAwait(false);
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
            return GetOperationTimeout(cloudToDeviceMethod.ConnectionTimeout, cloudToDeviceMethod.ResponseTimeout);
        }

        private static TimeSpan GetInitiateStreamOperationTimeout(DeviceStreamRequest initiation)
        {
            return GetOperationTimeout(initiation.ConnectionTimeout, initiation.ResponseTimeout);
        }

        private static TimeSpan GetOperationTimeout(TimeSpan connectionTimeout, TimeSpan responseTimeout)
        {
            TimeSpan timeout = TimeSpan.FromSeconds(15); // For wire time
            timeout += connectionTimeout;
            timeout += responseTimeout;
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

        static Uri GetDeviceStreamUri(string deviceId, string streamName)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceStreamUriFormat.FormatInvariant(deviceId, streamName), UriKind.Relative);
        }

        static Uri GetDeviceMethodUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(DeviceMethodUriFormat.FormatInvariant(deviceId), UriKind.Relative);
        }

        static Uri GetModuleMethodUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModuleMethodUriFormat.FormatInvariant(deviceId, moduleId), UriKind.Relative);
        }

        static Uri GetModuleStreamUri(string deviceId, string moduleId, string streamName)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(ModuleStreamUriFormat.FormatInvariant(deviceId, moduleId, streamName), UriKind.Relative);
        }
    }
}
