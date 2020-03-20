// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class HttpTransportHandler : TransportHandler
    {
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan s_defaultMethodOperationTimeout = TimeSpan.FromSeconds(100);

        private static readonly IDictionary<string, string> s_mapMessageProperties2HttpHeaders = new Dictionary<string, string>
        {
            { MessageSystemPropertyNames.Ack, CustomHeaderConstants.Ack },
            { MessageSystemPropertyNames.CorrelationId, CustomHeaderConstants.CorrelationId },
            { MessageSystemPropertyNames.ExpiryTimeUtc, CustomHeaderConstants.ExpiryTimeUtc },
            { MessageSystemPropertyNames.MessageId, CustomHeaderConstants.MessageId },
            { MessageSystemPropertyNames.Operation, CustomHeaderConstants.Operation },
            { MessageSystemPropertyNames.To, CustomHeaderConstants.To },
            { MessageSystemPropertyNames.UserId, CustomHeaderConstants.UserId },
            { MessageSystemPropertyNames.MessageSchema, CustomHeaderConstants.MessageSchema },
            { MessageSystemPropertyNames.CreationTimeUtc, CustomHeaderConstants.CreationTimeUtc },
            { MessageSystemPropertyNames.ContentType, CustomHeaderConstants.ContentType },
            { MessageSystemPropertyNames.ContentEncoding, CustomHeaderConstants.ContentEncoding },
            { MessageSystemPropertyNames.InterfaceId, CustomHeaderConstants.InterfaceId }
        };

        private static readonly Dictionary<string, string> s_reject = new Dictionary<string, string>
        {
            { "reject", null }
        };

        private readonly IHttpClientHelper _httpClientHelper;
        private readonly string _deviceId;
        private readonly string _moduleId;

        internal HttpTransportHandler(IPipelineContext context, IotHubConnectionString iotHubConnectionString, Http1TransportSettings transportSettings, HttpClientHandler httpClientHandler = null)
            : base(context, transportSettings)
        {
            _deviceId = iotHubConnectionString.DeviceId;
            _moduleId = iotHubConnectionString.ModuleId;

            ProductInfo productInfo = context.Get<ProductInfo>();
            _httpClientHelper = new HttpClientHelper(
                iotHubConnectionString.HttpsEndpoint,
                iotHubConnectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                null,
                transportSettings.ClientCertificate,
                httpClientHandler,
                productInfo,
                transportSettings.Proxy);
        }

        public override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            Debug.Assert(message != null);
            cancellationToken.ThrowIfCancellationRequested();

            var customHeaders = new Dictionary<string, string>(message.SystemProperties.Count + message.Properties.Count);
            foreach (KeyValuePair<string, object> property in message.SystemProperties)
            {
                string strValue = property.Value is DateTime
                    ? ((DateTime)property.Value).ToString("o", CultureInfo.InvariantCulture)
                    : property.Value.ToString();
                customHeaders.Add(s_mapMessageProperties2HttpHeaders[property.Key], strValue);
            }

            foreach (KeyValuePair<string, string> property in message.Properties)
            {
                customHeaders.Add(CustomHeaderConstants.HttpAppPropertyPrefix + property.Key, property.Value);
            }

            return _httpClientHelper.PostAsync(
                GetRequestUri(_deviceId, CommonConstants.DeviceEventPathTemplate, null),
                message.GetBytes(),
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        public override Task SendEventAsync(IEnumerable<Message> messages, CancellationToken cancellationToken)
        {
            if (messages == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(messages));
            }

            cancellationToken.ThrowIfCancellationRequested();

            IDictionary<string, string> customHeaders = PrepareCustomHeaders(CommonConstants.DeviceEventPathTemplate.FormatInvariant(_deviceId), string.Empty, CommonConstants.DeviceToCloudOperation);

            string body = ToJson(messages);
            return _httpClientHelper.PostAsync(
                GetRequestUri(_deviceId, CommonConstants.DeviceEventPathTemplate, null),
                body,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        public override Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Device twins are only supported with Mqtt protocol.");
        }

        public override async Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDictionary<string, string> customHeaders = PrepareCustomHeaders(CommonConstants.DeviceBoundPathTemplate.FormatInvariant(_deviceId), null, CommonConstants.CloudToDeviceOperation);
            IDictionary<string, string> queryValueDictionary =
                new Dictionary<string, string>() { { CustomHeaderConstants.MessageLockTimeout, s_defaultOperationTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) } };

            HttpResponseMessage responseMessage = await _httpClientHelper
                .GetAsync<HttpResponseMessage>(
                    GetRequestUri(_deviceId, CommonConstants.DeviceBoundPathTemplate, queryValueDictionary),
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    customHeaders,
                    true,
                    cancellationToken)
                .ConfigureAwait(false);

            if (responseMessage == null || responseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            responseMessage.Headers.TryGetValues(CustomHeaderConstants.MessageId, out IEnumerable<string> messageId);
            responseMessage.Headers.TryGetValues(HttpResponseHeader.ETag.ToString(), out IEnumerable<string> lockToken);
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.EnqueuedTime, out IEnumerable<string> enqueuedTime);
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.DeliveryCount, out IEnumerable<string> deliveryCountAsStr);
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.ExpiryTimeUtc, out IEnumerable<string> expiryTime);
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.CorrelationId, out IEnumerable<string> correlationId);
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.SequenceNumber, out IEnumerable<string> sequenceNumber);

            byte[] byteContent = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            Message message = byteContent != null
                ? new Message(byteContent)
                : new Message();

            message.MessageId = messageId?.First();
            message.LockToken = lockToken?.First().Trim('\"');

            if (enqueuedTime != null)
            {
                if (DateTime.TryParse(enqueuedTime.First(), out DateTime enqueuedTimeUtc))
                {
                    message.EnqueuedTimeUtc = enqueuedTimeUtc;
                }
            }

            if (deliveryCountAsStr != null)
            {
                if (byte.TryParse(deliveryCountAsStr.First(), out byte deliveryCount))
                {
                    message.DeliveryCount = deliveryCount;
                }
            }

            if (expiryTime != null)
            {
                if (DateTime.TryParse(expiryTime.First(), out DateTime absoluteExpiryTime))
                {
                    message.ExpiryTimeUtc = absoluteExpiryTime;
                }
            }

            message.CorrelationId = correlationId?.First();
            message.SequenceNumber = sequenceNumber == null
                ? 0
                : Convert.ToUInt64(sequenceNumber.First(), CultureInfo.InvariantCulture);

            // Read custom headers and map them to properties.
            foreach (KeyValuePair<string, IEnumerable<string>> keyValue in responseMessage.Headers)
            {
                if (keyValue.Key.StartsWith(CustomHeaderConstants.HttpAppPropertyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    message.Properties.Add(keyValue.Key.Substring(CustomHeaderConstants.HttpAppPropertyPrefix.Length), keyValue.Value.First());
                }
            }

            return message;
        }

        public override async Task<Message> ReceiveAsync(TimeoutHelper timeoutHelper)
        {
            TimeSpan timeout = timeoutHelper.RemainingTime();
            if (timeout > TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(nameof(timeoutHelper), "Http Protocol does not support a non-zero receive timeout");
            }

            using var cts = new CancellationTokenSource(s_defaultOperationTimeout);
            return await ReceiveAsync(cts.Token).ConfigureAwait(false);
        }

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathCompleteTemplate.FormatInvariant(_deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            var eTag = new ETagHolder { ETag = lockToken };

            return _httpClientHelper.DeleteAsync(
                GetRequestUri(_deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}".FormatInvariant(lockToken), null),
                eTag,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathAbandonTemplate.FormatInvariant(_deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            // Even though If-Match is not a customHeader, add it here for convenience
            customHeaders.Add(HttpRequestHeader.IfMatch.ToString(), lockToken);

            return _httpClientHelper.PostAsync(
                GetRequestUri(_deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}/abandon".FormatInvariant(lockToken), null),
                (object)null,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathRejectTemplate.FormatInvariant(_deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            var eTag = new ETagHolder { ETag = lockToken };

            return _httpClientHelper.DeleteAsync(
                GetRequestUri(
                    _deviceId,
                    CommonConstants.DeviceBoundPathTemplate + "/{0}".FormatInvariant(lockToken),
                    s_reject),
                eTag,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        internal Task<MethodInvokeResponse> InvokeMethodAsync(MethodInvokeRequest methodInvokeRequest, Uri uri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_moduleId))
            {
                throw new InvalidOperationException("ModuleId is required.");
            }
            _ = GetInvokeDeviceMethodOperationTimeout(methodInvokeRequest);
            var customHeaders = new Dictionary<string, string>
            {
                { CustomHeaderConstants.ModuleId, $"{_deviceId}/{_moduleId}" }
            };

            return _httpClientHelper.PostAsync<MethodInvokeRequest, MethodInvokeResponse>(
                uri,
                methodInvokeRequest,
                null,
                customHeaders,
                cancellationToken);
        }

        internal async Task UploadToBlobAsync(string blobName, Stream source, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var fileUploadRequest = new FileUploadRequest()
            {
                BlobName = blobName
            };

            FileUploadResponse fileUploadResponse = await _httpClientHelper
                .PostAsync<FileUploadRequest, FileUploadResponse>(
                    GetRequestUri(_deviceId, CommonConstants.BlobUploadPathTemplate, null),
                    fileUploadRequest,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            string putString = string.Format(
                CultureInfo.InvariantCulture,
                "https://{0}/{1}/{2}{3}",
                fileUploadResponse.HostName,
                fileUploadResponse.ContainerName,
                Uri.EscapeDataString(fileUploadResponse.BlobName), // Pass URL encoded device name and blob name to support special characters
                fileUploadResponse.SasToken);

            var notification = new FileUploadNotificationResponse();

            try
            {
                // 2. Use SAS URI to send data to Azure Storage Blob (PUT)
                var blob = new CloudBlockBlob(new Uri(putString));
                Task uploadTask = blob.UploadFromStreamAsync(source);
                await uploadTask.ConfigureAwait(false);

                notification.CorrelationId = fileUploadResponse.CorrelationId;
                notification.IsSuccess = uploadTask.IsCompleted;
                notification.StatusCode = uploadTask.IsCompleted ? 0 : -1;
                notification.StatusDescription = uploadTask.IsCompleted
                    ? null
                    : "Failed to upload to storage.";

                // 3. POST to IoTHub with upload status
                await _httpClientHelper
                    .PostAsync(
                        GetRequestUri(_deviceId, CommonConstants.BlobUploadStatusPathTemplate + "notifications", null),
                        notification,
                        ExceptionHandlingHelper.GetDefaultErrorMapping(),
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException) && !ex.IsFatal())
            {
                // 3. POST to IoTHub with upload status
                notification.IsSuccess = false;
                notification.StatusCode = -1;
                notification.StatusDescription = ex.Message;

                await _httpClientHelper
                    .PostAsync(
                        GetRequestUri(_deviceId, CommonConstants.BlobUploadStatusPathTemplate + "notifications/" + fileUploadResponse.CorrelationId, null),
                        notification,
                        ExceptionHandlingHelper.GetDefaultErrorMapping(),
                        null,
                        cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _httpClientHelper?.Dispose();
            }

            base.Dispose(disposing);
        }

        private static TimeSpan GetInvokeDeviceMethodOperationTimeout(MethodInvokeRequest methodInvokeRequest)
        {
            // For InvokeDeviceMethod, we need to take into account the timeouts specified
            // for the Device to connect and send a response. We also need to take into account
            // the transmission time for the request send/receive
            var timeout = TimeSpan.FromSeconds(15); // For wire time
            timeout += TimeSpan.FromSeconds(methodInvokeRequest.ConnectionTimeoutInSeconds ?? 0);
            timeout += TimeSpan.FromSeconds(methodInvokeRequest.ResponseTimeoutInSeconds ?? 0);
            return timeout > s_defaultMethodOperationTimeout
                ? timeout
                : s_defaultMethodOperationTimeout;
        }

        private static IDictionary<string, string> PrepareCustomHeaders(string toHeader, string messageId, string operation)
        {
            var customHeaders = new Dictionary<string, string>
            {
                { CustomHeaderConstants.To, toHeader },
                { CustomHeaderConstants.Operation, operation },
            };

            if (!string.IsNullOrEmpty(messageId))
            {
                customHeaders.Add(CustomHeaderConstants.MessageId, messageId);
            }

            return customHeaders;
        }

        private static Uri GetRequestUri(string deviceId, string path, IDictionary<string, string> queryValueDictionary)
        {
            deviceId = WebUtility.UrlEncode(deviceId);

            var stringBuilder = new StringBuilder("{0}?{1}".FormatInvariant(path.FormatInvariant(deviceId), ClientApiVersionHelper.ApiVersionQueryString));

            if (queryValueDictionary != null)
            {
                foreach (KeyValuePair<string, string> queryValue in queryValueDictionary)
                {
                    if (string.IsNullOrEmpty(queryValue.Value))
                    {
                        stringBuilder.Append("&{0}".FormatInvariant(queryValue.Key));
                    }
                    else
                    {
                        stringBuilder.Append("&{0}={1}".FormatInvariant(queryValue.Key, queryValue.Value));
                    }
                }
            }

            return new Uri(stringBuilder.ToString(), UriKind.Relative);
        }

        private static string ToJson(IEnumerable<Message> messages)
        {
            using var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw);

            writer.WriteStartArray();

            foreach (Message message in messages)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("body");

                // always encode body as Base64 string
                writer.WriteValue(Convert.ToBase64String(message.GetBytes()));

                // skip base64Encoded property since the default is true

                writer.WritePropertyName("properties");
                writer.WriteStartObject();

                foreach (KeyValuePair<string, object> property in message.SystemProperties)
                {
                    writer.WritePropertyName(s_mapMessageProperties2HttpHeaders[property.Key]);
                    writer.WriteValue(property.Value);
                }

                foreach (KeyValuePair<string, string> property in message.Properties)
                {
                    writer.WritePropertyName(CustomHeaderConstants.HttpAppPropertyPrefix + property.Key);
                    writer.WriteValue(property.Value);
                }

                writer.WriteEndObject();
                writer.WriteEndObject();
            }

            writer.WriteEndArray();

            return sw.ToString();
        }
    }
}
