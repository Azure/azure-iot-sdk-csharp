﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Client.Exceptions;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage;
using System.Diagnostics;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class HttpTransportHandler : TransportHandler
    {
        static readonly TimeSpan DefaultOperationTimeout = TimeSpan.FromSeconds(60);
        static readonly TimeSpan DefaultMethodOperationTimeout = TimeSpan.FromSeconds(100);
        static readonly IDictionary<string, string> MapMessageProperties2HttpHeaders = new Dictionary<string, string>
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

        readonly IHttpClientHelper httpClientHelper;
        readonly string deviceId;
        readonly string moduleId;

        internal HttpTransportHandler(IPipelineContext context, IotHubConnectionString iotHubConnectionString, Http1TransportSettings transportSettings, HttpClientHandler httpClientHandler = null)
            :base(context, transportSettings)
        {
            ProductInfo productInfo = context.Get<ProductInfo>();
            this.deviceId = iotHubConnectionString.DeviceId;
            this.moduleId = iotHubConnectionString.ModuleId;
            this.httpClientHelper = new HttpClientHelper(
                iotHubConnectionString.HttpsEndpoint,
                iotHubConnectionString,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                DefaultOperationTimeout,
                null,
                transportSettings.ClientCertificate,
                httpClientHandler,
                productInfo,
                transportSettings.Proxy);
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
            foreach (var property in message.SystemProperties)
            {
                string strValue = property.Value is DateTime ? ((DateTime)property.Value).ToString("o") : property.Value.ToString();
                customHeaders.Add(MapMessageProperties2HttpHeaders[property.Key], strValue);
            }

            foreach (var property in message.Properties)
            {
                customHeaders.Add(CustomHeaderConstants.HttpAppPropertyPrefix + property.Key, property.Value);
            }

            return this.httpClientHelper.PostAsync<byte[]>(
                GetRequestUri(this.deviceId, CommonConstants.DeviceEventPathTemplate, null),
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
            
            var customHeaders = PrepareCustomHeaders(CommonConstants.DeviceEventPathTemplate.FormatInvariant(this.deviceId), string.Empty, CommonConstants.DeviceToCloudOperation);

            string body = ToJson(messages);
            return this.httpClientHelper.PostAsync<string>(
                        GetRequestUri(this.deviceId, CommonConstants.DeviceEventPathTemplate, null),
                        body,
                        ExceptionHandlingHelper.GetDefaultErrorMapping(),
                        customHeaders,
                        cancellationToken);
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (disposing)
                {
                    this.httpClientHelper?.Dispose();
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        internal async Task UploadToBlobAsync(string blobName, Stream source, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var fileUploadRequest = new FileUploadRequest()
            {
                BlobName = blobName
            };

            var fileUploadResponse = await this.httpClientHelper.PostAsync<FileUploadRequest, FileUploadResponse>(
            GetRequestUri(this.deviceId, CommonConstants.BlobUploadPathTemplate, null),
            fileUploadRequest,
            ExceptionHandlingHelper.GetDefaultErrorMapping(),
            null,
            cancellationToken).ConfigureAwait(false);

            string putString = String.Format(
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
                CloudBlockBlob blob = new CloudBlockBlob(new Uri(putString));
                var uploadTask = blob.UploadFromStreamAsync(source);
                await uploadTask.ConfigureAwait(false);

                notification.CorrelationId = fileUploadResponse.CorrelationId;
                notification.IsSuccess = uploadTask.IsCompleted;
                notification.StatusCode = uploadTask.IsCompleted ? 0 : -1;
                notification.StatusDescription = uploadTask.IsCompleted ? null : "Failed to upload to storage.";

                // 3. POST to IoTHub with upload status
                await this.httpClientHelper.PostAsync<FileUploadNotificationResponse>(
                    GetRequestUri(this.deviceId, CommonConstants.BlobUploadStatusPathTemplate + "notifications", null),
                    notification,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    null,
                    cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (!ex.IsFatal())
            {
                // 3. POST to IoTHub with upload status
                notification.IsSuccess = false;
                notification.StatusCode = -1;
                notification.StatusDescription = ex.Message;

                await this.httpClientHelper.PostAsync<FileUploadNotificationResponse>(
                    GetRequestUri(this.deviceId, CommonConstants.BlobUploadStatusPathTemplate + "notifications/" + fileUploadResponse.CorrelationId, null),
                    notification,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    null,
                    cancellationToken).ConfigureAwait(false);

                throw;
            }
        }

        public override Task<Twin> SendTwinGetAsync(CancellationToken cancellationToken)
        {
            throw new NotImplementedException("Device twins are only supported with Mqtt protocol.");
        }

        public override Task<Message> ReceiveAsync(CancellationToken cancellationToken)
        {
            return this.ReceiveAsync(TimeSpan.Zero, cancellationToken);
        }

        public override async Task<Message> ReceiveAsync(TimeSpan timeout, CancellationToken cancellationToken)
        {
            // Long-polling is not supported
            if (!TimeSpan.Zero.Equals(timeout))
            {
                throw new ArgumentOutOfRangeException(nameof(timeout), "Http Protocol does not support a non-zero receive timeout");
            }

            cancellationToken.ThrowIfCancellationRequested();

            IDictionary<string, string> customHeaders = PrepareCustomHeaders(CommonConstants.DeviceBoundPathTemplate.FormatInvariant(this.deviceId), null, CommonConstants.CloudToDeviceOperation);
            IDictionary<string, string> queryValueDictionary =
                new Dictionary<string, string>() { { CustomHeaderConstants.MessageLockTimeout, DefaultOperationTimeout.TotalSeconds.ToString(CultureInfo.InvariantCulture) } };

            HttpResponseMessage responseMessage = await this.httpClientHelper.GetAsync<HttpResponseMessage>(
                GetRequestUri(this.deviceId, CommonConstants.DeviceBoundPathTemplate, queryValueDictionary),
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                true,
                cancellationToken).ConfigureAwait(false);

            if (responseMessage == null || responseMessage.StatusCode == HttpStatusCode.NoContent)
            {
                return null;
            }

            IEnumerable<string> messageId;
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.MessageId, out messageId);

            IEnumerable<string> lockToken;
            responseMessage.Headers.TryGetValues(HttpResponseHeader.ETag.ToString(), out lockToken);

            IEnumerable<string> enqueuedTime;
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.EnqueuedTime, out enqueuedTime);

            IEnumerable<string> deliveryCountAsStr;
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.DeliveryCount, out deliveryCountAsStr);

            IEnumerable<string> expiryTime;
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.ExpiryTimeUtc, out expiryTime);

            IEnumerable<string> correlationId;
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.CorrelationId, out correlationId);

            IEnumerable<string> sequenceNumber;
            responseMessage.Headers.TryGetValues(CustomHeaderConstants.SequenceNumber, out sequenceNumber);

            byte[] byteContent = await responseMessage.Content.ReadAsByteArrayAsync().ConfigureAwait(false);

            var message = byteContent != null ? new Message(byteContent) : new Message();

            message.MessageId = messageId != null ? messageId.First() : null;
            message.LockToken = lockToken != null ? lockToken.First().Trim('\"') : null;

            if (enqueuedTime != null)
            {
                DateTime enqueuedTimeUtc;
                if (DateTime.TryParse(enqueuedTime.First(), out enqueuedTimeUtc))
                {
                    message.EnqueuedTimeUtc = enqueuedTimeUtc;
                }
            }

            if (deliveryCountAsStr != null)
            {
                byte deliveryCount;
                if (byte.TryParse(deliveryCountAsStr.First(), out deliveryCount))
                {
                    message.DeliveryCount = deliveryCount;
                }
            }

            if (expiryTime != null)
            {
                DateTime absoluteExpiryTime;
                if (DateTime.TryParse(expiryTime.First(), out absoluteExpiryTime))
                {
                    message.ExpiryTimeUtc = absoluteExpiryTime;
                }
            }

            message.CorrelationId = correlationId != null ? correlationId.First() : null;
            message.SequenceNumber = sequenceNumber != null ? Convert.ToUInt64(sequenceNumber.First()) : 0;

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

        public override Task CompleteAsync(string lockToken, CancellationToken cancellationToken)
        {
            var customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathCompleteTemplate.FormatInvariant(this.deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            var eTag = new ETagHolder { ETag = lockToken };

            return this.httpClientHelper.DeleteAsync(
                GetRequestUri(this.deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}".FormatInvariant(lockToken), null),
                eTag,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        public override Task AbandonAsync(string lockToken, CancellationToken cancellationToken)
        {
            var customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathAbandonTemplate.FormatInvariant(this.deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            // Even though If-Match is not a customHeader, add it here for convenience
            customHeaders.Add(HttpRequestHeader.IfMatch.ToString(), lockToken);

            return this.httpClientHelper.PostAsync(
                    GetRequestUri(this.deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}/abandon".FormatInvariant(lockToken), null),
                    (Object)null,
                    ExceptionHandlingHelper.GetDefaultErrorMapping(),
                    customHeaders,
                    cancellationToken);
        }

        public override Task RejectAsync(string lockToken, CancellationToken cancellationToken)
        {
            var customHeaders = PrepareCustomHeaders(
                CommonConstants.DeviceBoundPathRejectTemplate.FormatInvariant(this.deviceId, lockToken),
                null,
                CommonConstants.CloudToDeviceOperation);

            var eTag = new ETagHolder { ETag = lockToken };

            return this.httpClientHelper.DeleteAsync(
                GetRequestUri(this.deviceId, CommonConstants.DeviceBoundPathTemplate + "/{0}".FormatInvariant(lockToken), new Dictionary<string, string>
                {
                        { "reject", null }
                }),
                eTag,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                customHeaders,
                cancellationToken);
        }

        internal Task<MethodInvokeResponse> InvokeMethodAsync(MethodInvokeRequest methodInvokeRequest, Uri uri, CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(this.moduleId))
            {
                throw new InvalidOperationException("ModuleId is required.");
            }

            TimeSpan timeout = GetInvokeDeviceMethodOperationTimeout(methodInvokeRequest);
            var customHeaders = new Dictionary<string, string>
            {
                { CustomHeaderConstants.ModuleId, $"{this.deviceId}/{this.moduleId}" }
            };

            return this.httpClientHelper.PostAsync<MethodInvokeRequest, MethodInvokeResponse>(
                uri,
                methodInvokeRequest,
                null,
                customHeaders,
                cancellationToken);
        }

        static TimeSpan GetInvokeDeviceMethodOperationTimeout(MethodInvokeRequest methodInvokeRequest)
        {
            // For InvokeDeviceMethod, we need to take into account the timeouts specified
            // for the Device to connect and send a response. We also need to take into account
            // the transmission time for the request send/receive
            TimeSpan timeout = TimeSpan.FromSeconds(15); // For wire time
            timeout += TimeSpan.FromSeconds(methodInvokeRequest.ConnectionTimeoutInSeconds ?? 0);
            timeout += TimeSpan.FromSeconds(methodInvokeRequest.ResponseTimeoutInSeconds ?? 0);
            return timeout <= DefaultMethodOperationTimeout ? DefaultMethodOperationTimeout : timeout;
        }

        static IDictionary<string, string> PrepareCustomHeaders(string toHeader, string messageId, string operation)
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

        static Uri GetRequestUri(string deviceId, string path, IDictionary<string, string> queryValueDictionary)
        {
            deviceId = WebUtility.UrlEncode(deviceId);

            var stringBuilder = new StringBuilder("{0}?{1}".FormatInvariant(path.FormatInvariant(deviceId), ClientApiVersionHelper.ApiVersionQueryString));

            if (queryValueDictionary != null)
            {
                foreach (var queryValue in queryValueDictionary)
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

        static string ToJson(IEnumerable<Message> messages)
        {
            using (var sw = new StringWriter())
            using (var writer = new JsonTextWriter(sw))
            {
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

                    foreach (var property in message.SystemProperties)
                    {
                        writer.WritePropertyName(MapMessageProperties2HttpHeaders[property.Key]);
                        writer.WriteValue(property.Value);
                    }

                    foreach (var property in message.Properties)
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

        static string ToJson(IEnumerable<string> messages)
        {
            using (var sw = new StringWriter())
            using (var writer = new JsonTextWriter(sw))
            {
                // [
                writer.WriteStartArray();

                foreach (var message in messages)
                {
                    // {
                    writer.WriteStartObject();

                    // "body" : "{\"my\": \"message\", \"is\": \"json\"}"
                    writer.WritePropertyName("body");
                    writer.WriteValue(message);

                    // "base64Encoded":false
                    writer.WritePropertyName("base64Encoded");
                    writer.WriteValue(false);

                    // }
                    writer.WriteEndObject();
                }

                writer.WriteEndArray();
                // ]

                return sw.ToString();
            }
        }

        static string ToJson(IEnumerable<Tuple<string, IDictionary<string, string>>> messages)
        {
            using (var sw = new StringWriter())
            using (var writer = new JsonTextWriter(sw))
            {
                // [
                writer.WriteStartArray();

                foreach (var message in messages)
                {
                    // {
                    writer.WriteStartObject();

                    // "body" : "{\"my\": \"message\", \"is\": \"json\"}"
                    writer.WritePropertyName("body");
                    writer.WriteValue(message.Item1);

                    // "base64Encoded":false
                    writer.WritePropertyName("base64Encoded");
                    writer.WriteValue(false);

                    // "properties" :
                    writer.WritePropertyName("properties");

                    // {
                    writer.WriteStartObject();

                    foreach (var property in message.Item2)
                    {
                        writer.WritePropertyName(property.Key);
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
}
