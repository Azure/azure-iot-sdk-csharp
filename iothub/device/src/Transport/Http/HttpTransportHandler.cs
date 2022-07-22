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
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The final node in the handler chain, running operations on the HTTP transport.
    /// </summary>
    internal sealed class HttpTransportHandler : TransportHandler
    {
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan s_defaultMethodOperationTimeout = TimeSpan.FromSeconds(100);

        private static readonly IDictionary<string, string> s_mapMessagePropertiesToHttpHeaders = new Dictionary<string, string>
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
            { MessageSystemPropertyNames.InterfaceId, CustomHeaderConstants.InterfaceId },
            { MessageSystemPropertyNames.ComponentName, CustomHeaderConstants.ComponentName }
        };

        private static readonly IDictionary<string, string> s_mapHttpHeadersToMessageProperties = new Dictionary<string, string>
        {
            { HttpResponseHeader.ETag.ToString(), MessageSystemPropertyNames.LockToken },
            { CustomHeaderConstants.MessageId, MessageSystemPropertyNames.MessageId },
            { CustomHeaderConstants.SequenceNumber, MessageSystemPropertyNames.SequenceNumber },
            { CustomHeaderConstants.To, MessageSystemPropertyNames.To },
            { CustomHeaderConstants.ExpiryTimeUtc, MessageSystemPropertyNames.ExpiryTimeUtc },
            { CustomHeaderConstants.CorrelationId, MessageSystemPropertyNames.CorrelationId },
            { CustomHeaderConstants.UserId, MessageSystemPropertyNames.UserId },
            { CustomHeaderConstants.Ack, MessageSystemPropertyNames.Ack },
            { CustomHeaderConstants.EnqueuedTime, MessageSystemPropertyNames.EnqueuedTime },
            { CustomHeaderConstants.DeliveryCount, MessageSystemPropertyNames.DeliveryCount },
        };

        private readonly IHttpClientHelper _httpClientHelper;
        private readonly string _deviceId;
        private readonly string _moduleId;

        internal HttpTransportHandler(
            PipelineContext context,
            IotHubConnectionString iotHubConnectionString,
            Http1TransportSettings transportSettings,
            HttpClientHandler httpClientHandler = null,
            bool isClientPrimaryTransportHandler = false)
            : base(context, transportSettings)
        {
            ProductInfo productInfo = context.ProductInfo;
            _deviceId = iotHubConnectionString.DeviceId;
            _moduleId = iotHubConnectionString.ModuleId;
            _httpClientHelper = new HttpClientHelper(
                iotHubConnectionString.HttpsEndpoint,
                iotHubConnectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                null,
                transportSettings.ClientCertificate,
                httpClientHandler,
                productInfo,
                transportSettings.Proxy,
                isClientPrimaryTransportHandler);
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            return TaskHelpers.CompletedTask;
        }

        public override Task CloseAsync(CancellationToken cancellationToken)
        {
            return TaskHelpers.CompletedTask;
        }

        public override Task SendEventAsync(Message message, CancellationToken cancellationToken)
        {
            Debug.Assert(message != null);
            cancellationToken.ThrowIfCancellationRequested();

            var customHeaders = new Dictionary<string, string>(message.SystemProperties.Count + message.Properties.Count);
            foreach (KeyValuePair<string, object> property in message.SystemProperties)
            {
                string strValue = property.Value is DateTime time
                    ? time.ToString("o", CultureInfo.InvariantCulture)
                    : property.Value.ToString();
                customHeaders.Add(s_mapMessagePropertiesToHttpHeaders[property.Key], strValue);
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

            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceEventPathTemplate.FormatInvariant(_deviceId),
                string.Empty,
                CommonConstants.DeviceToCloudOperation);

            string body = ToJson(messages);
            return _httpClientHelper.PostAsync(
                GetRequestUri(_deviceId, CommonConstants.DeviceEventPathTemplate, null),
                body,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (Logging.IsEnabled)
                {
                    Logging.Enter(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(HttpTransportHandler)}.{nameof(Dispose)}");
                }

                if (!_disposed)
                {
                    base.Dispose(disposing);
                    if (disposing)
                    {
                        _httpClientHelper?.Dispose();
                    }

                    // the _disposed flag is inherited from the base class DefaultDelegatingHandler and is finally set to null there.
                }
            }
            finally
            {
                if (Logging.IsEnabled)
                {
                    Logging.Exit(this, $"{nameof(DefaultDelegatingHandler)}.Disposed={_disposed}; disposing={disposing}", $"{nameof(HttpTransportHandler)}.{nameof(Dispose)}");
                }
            }
        }

        internal async Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _httpClientHelper
                .PostAsync<FileUploadSasUriRequest, FileUploadSasUriResponse>(
                    GetRequestUri(_deviceId, CommonConstants.BlobUploadPathTemplate, null),
                    request,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _httpClientHelper
                .PostAsync(
                    GetRequestUri(_deviceId, CommonConstants.BlobUploadStatusPathTemplate + "notifications", null),
                    notification,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public override Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException(
                "Device twins are not supported over HTTP; they are only supported over the MQTT and AMQP protocols.");
        }

        public override async Task<Message> ReceiveMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathTemplate.FormatInvariant(_deviceId),
                null,
                CommonConstants.CloudToDeviceOperation);
            var queryValueDictionary = new Dictionary<string, string>
            {
                { CustomHeaderConstants.MessageLockTimeout, s_defaultOperationTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) }
            };

            HttpResponseMessage responseMessage = await _httpClientHelper
                .GetAsync<HttpResponseMessage>(
                    GetRequestUri(_deviceId, CommonConstants.DeviceBoundPathTemplate, queryValueDictionary),
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    customHeaders,
                    true,
                    cancellationToken)
                .ConfigureAwait(false);

            if (responseMessage == null
                || responseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            byte[] byteContent = await responseMessage.Content
                .ReadHttpContentAsByteArrayAsync(cancellationToken)
                .ConfigureAwait(false);

            Message message = byteContent == null
                ? new Message()
                : new Message(byteContent);

            // Read Http headers and map them to message system properties.
            foreach (KeyValuePair<string, IEnumerable<string>> header in responseMessage.Headers)
            {
                string headerKey = header.Key;

                if (s_mapHttpHeadersToMessageProperties.ContainsKey(headerKey))
                {
                    string messagePropertyKey = s_mapHttpHeadersToMessageProperties[headerKey];
                    object messagePropertyValue = ConvertToMessageSystemProperty(messagePropertyKey, header.Value);
                    if (messagePropertyValue != null)
                    {
                        message.SystemProperties[messagePropertyKey] = messagePropertyValue;
                    }
                }
            }

            // Read custom headers and map them to properties.
            foreach (KeyValuePair<string, IEnumerable<string>> keyValue in responseMessage.Headers)
            {
                if (keyValue.Key.StartsWith(CustomHeaderConstants.HttpAppPropertyPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    message.Properties.Add(
                        keyValue.Key.Substring(CustomHeaderConstants.HttpAppPropertyPrefix.Length),
                        keyValue.Value.First());
                }
            }

            return message;
        }

        public override Task EnableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException("HTTP protocol does not support setting callbacks for receiving messages." +
                " You can either call DeviceClient.ReceiveAsync() to wait and receive messages," +
                " or set the callback over MQTT or AMQP.");
        }

        public override Task DisableReceiveMessageAsync(CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            throw new NotSupportedException("HTTP protocol does not support setting callbacks for receiving messages." +
                " You can either call DeviceClient.ReceiveAsync() to wait and receive messages," +
                " or set the callback over MQTT or AMQP.");
        }

        public override async Task CompleteMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathCompleteTemplate.FormatInvariant(_deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            var eTag = new ETagHolder { ETag = lockToken };

            await _httpClientHelper
                .DeleteAsync(
                    GetRequestUri(_deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}".FormatInvariant(lockToken), null),
                    eTag,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    customHeaders,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task AbandonMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathAbandonTemplate.FormatInvariant(_deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            // Even though If-Match is not a customHeader, add it here for convenience
            customHeaders.Add(HttpRequestHeader.IfMatch.ToString(), lockToken);

            await _httpClientHelper
                .PostAsync(
                    GetRequestUri(_deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}/abandon".FormatInvariant(lockToken), null),
                    (object)null,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    customHeaders,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        public override async Task RejectMessageAsync(string lockToken, CancellationToken cancellationToken)
        {
            IDictionary<string, string> customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathRejectTemplate.FormatInvariant(_deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            var eTag = new ETagHolder { ETag = lockToken };

            await _httpClientHelper
                .DeleteAsync(
                    GetRequestUri(
                        _deviceId,
                        CommonConstants.DeviceBoundPathTemplate + "/{0}".FormatInvariant(lockToken),
                        new Dictionary<string, string> { { "reject", null } }),
                    eTag,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    customHeaders,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // This is for invoking methods from an edge module to another edge device or edge module.
        internal async Task<MethodInvokeResponse> InvokeMethodAsync(
            MethodInvokeRequest methodInvokeRequest,
            Uri uri,
            CancellationToken cancellationToken)
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

            return await _httpClientHelper
                .PostAsync<MethodInvokeRequest, MethodInvokeResponse>(
                    uri,
                    methodInvokeRequest,
                    null,
                    customHeaders,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        private static TimeSpan GetInvokeDeviceMethodOperationTimeout(MethodInvokeRequest methodInvokeRequest)
        {
            // For InvokeDeviceMethod, we need to take into account the timeouts specified
            // for the Device to connect and send a response. We also need to take into account
            // the transmission time for the request send/receive
            var timeout = TimeSpan.FromSeconds(15); // For wire time
            timeout += TimeSpan.FromSeconds(methodInvokeRequest.ConnectionTimeoutInSeconds ?? 0);
            timeout += TimeSpan.FromSeconds(methodInvokeRequest.ResponseTimeoutInSeconds ?? 0);
            return timeout <= s_defaultMethodOperationTimeout
                ? s_defaultMethodOperationTimeout
                : timeout;
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

        private static object ConvertToMessageSystemProperty(string messagePropertyName, IEnumerable<string> messagePropertyValues)
        {
            string propertyValue = messagePropertyValues?.First();

            switch (messagePropertyName)
            {
                case MessageSystemPropertyNames.LockToken:
                    return propertyValue.Trim('\"');

                case MessageSystemPropertyNames.EnqueuedTime:
                case MessageSystemPropertyNames.ExpiryTimeUtc:
                    return DateTime.TryParse(propertyValue, out DateTime dateTime) ? dateTime : (object)null;

                case MessageSystemPropertyNames.SequenceNumber:
                    return propertyValue == null ? 0 : Convert.ToUInt64(propertyValue, CultureInfo.InvariantCulture);

                case MessageSystemPropertyNames.DeliveryCount:
                    return byte.TryParse(propertyValue, out byte deliveryCount) ? deliveryCount : (object)null;

                default:
                    return propertyValue;
            }
        }

        private static Uri GetRequestUri(string deviceId, string path, IDictionary<string, string> queryValueDictionary)
        {
            deviceId = WebUtility.UrlEncode(deviceId);

            var stringBuilder = new StringBuilder("{0}?{1}".FormatInvariant(
                path.FormatInvariant(deviceId),
                ClientApiVersionHelper.ApiVersionQueryStringLatest));

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

            // [
            writer.WriteStartArray();

            foreach (Message message in messages)
            {
                // {
                writer.WriteStartObject();

                // always encode body as Base64 string
                writer.WritePropertyName("body");
                writer.WriteValue(Convert.ToBase64String(message.GetBytes()));

                // skip base64Encoded property since the default is true

                // "properties" :
                writer.WritePropertyName("properties");

                // {
                writer.WriteStartObject();

                foreach (KeyValuePair<string, object> property in message.SystemProperties)
                {
                    writer.WritePropertyName(s_mapMessagePropertiesToHttpHeaders[property.Key]);
                    writer.WriteValue(property.Value);
                }

                foreach (KeyValuePair<string, string> property in message.Properties)
                {
                    writer.WritePropertyName(CustomHeaderConstants.HttpAppPropertyPrefix + property.Key);
                    writer.WriteValue(property.Value);
                }

                // }
                writer.WriteEndObject();

                // }
                writer.WriteEndObject();
            }

            writer.WriteEndArray();
            // ]

            return sw.ToString();
        }
    }
}
