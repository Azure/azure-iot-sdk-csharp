// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.Exceptions;

namespace Microsoft.Azure.Devices.Client.Transport
{
    /// <summary>
    /// The final node in the handler chain, running operations on the HTTP transport.
    /// </summary>
    internal sealed class HttpTransportHandler : TransportHandler
    {
        public const string ModuleId = "x-ms-edge-moduleId";
        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan s_defaultMethodOperationTimeout = TimeSpan.FromSeconds(100);

        private readonly HttpClientHelper _httpClientHelper;
        private readonly string _deviceId;
        private readonly string _moduleId;

        internal HttpTransportHandler(
            PipelineContext context,
            IotHubClientHttpSettings transportSettings,
            HttpClientHandler httpClientHandler = null)
            : base(context, transportSettings)
        {
            var additionalClientInformation = new AdditionalClientInformation
            {
                ProductInfo = context.ProductInfo,
                ModelId = context.ModelId,
            };

            _deviceId = context.IotHubConnectionCredentials.DeviceId;
            _moduleId = context.IotHubConnectionCredentials.ModuleId;
            Uri httpsEndpoint = new UriBuilder(Uri.UriSchemeHttps, context.IotHubConnectionCredentials.HostName).Uri;
            _httpClientHelper = new HttpClientHelper(
                httpsEndpoint,
                context.IotHubConnectionCredentials,
                additionalClientInformation,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                null,
                httpClientHandler,
                transportSettings);
        }

        internal async Task<FileUploadSasUriResponse> GetFileUploadSasUriAsync(FileUploadSasUriRequest request, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return await _httpClientHelper
                .PostAsync<FileUploadSasUriRequest, FileUploadSasUriResponse>(
                    GetRequestUri(_deviceId, CommonConstants.BlobUploadPathTemplate, null),
                    request,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        internal async Task CompleteFileUploadAsync(FileUploadCompletionNotification notification, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            await _httpClientHelper
                .PostAsync<FileUploadCompletionNotification, Task>(
                    GetRequestUri(_deviceId, CommonConstants.BlobUploadStatusPathTemplate + "notifications", null),
                    notification,
                    null,
                    cancellationToken)
                .ConfigureAwait(false);
        }

        // This is for invoking methods from an edge module to another edge device or edge module.
        internal async Task<DirectMethodResponse> InvokeMethodAsync(
            DirectMethodRequest methodInvokeRequest,
            Uri uri,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(_moduleId))
            {
                throw new InvalidOperationException("ModuleId is required.");
            }

            var customHeaders = new Dictionary<string, string>
            {
                { ModuleId, $"{_deviceId}/{_moduleId}" }
            };

            return await _httpClientHelper
                .PostAsync<DirectMethodRequest, DirectMethodResponse>(
                    uri,
                    methodInvokeRequest,
                    customHeaders,
                    cancellationToken)
                .ConfigureAwait(false);
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
    }
}
