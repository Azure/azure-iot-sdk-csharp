// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication.Transport
{
    internal class HttpRequestResponseSerializer
    {
        private const char Space = ' ';
        private const char CarriageReturn = '\r';
        private const char LineFeed = '\n';
        private const char ProtocolVersionSeparator = '/';
        private const string Protocol = "HTTP";
        private const char HeaderSeparator = ':';
        private const string ContentLengthHeaderName = "content-length";

        internal static byte[] SerializeRequest(HttpRequestMessage request)
        {
            if (request == null)
            {
                throw new ArgumentNullException(nameof(request));
            }
            if (request.RequestUri == null)
            {
                throw new ArgumentNullException(nameof(request.RequestUri));
            }

            PreProcessRequest(request);

            var builder = new StringBuilder();
            // request-line   = method SP request-target SP HTTP-version CRLF
            builder.Append(request.Method);
            builder.Append(Space);
            builder.Append(request.RequestUri.IsAbsoluteUri
                ? request.RequestUri.PathAndQuery
                : Uri.EscapeUriString(request.RequestUri.ToString()));
            builder.Append(Space);
            builder.Append($"{Protocol}{ProtocolVersionSeparator}");
            builder.Append(new Version(1, 1).ToString(2));
            builder.Append(CarriageReturn);
            builder.Append(LineFeed);

            // Headers
            builder.Append(request.Headers);

            if (request.Content != null)
            {
                long? contentLength = request.Content.Headers.ContentLength;
                if (contentLength.HasValue)
                {
                    request.Content.Headers.ContentLength = contentLength.Value;
                }

                builder.Append(request.Content.Headers);
            }

            // Headers end
            builder.Append(CarriageReturn);
            builder.Append(LineFeed);

            return Encoding.ASCII.GetBytes(builder.ToString());
        }

        internal static async Task<HttpResponseMessage> DeserializeResponseAsync(HttpBufferedStream bufferedStream, CancellationToken cancellationToken)
        {
            var httpResponse = new HttpResponseMessage();

            await SetResponseStatusLineAsync(httpResponse, bufferedStream, cancellationToken).ConfigureAwait(false);
            await SetHeadersAndContentAsync(httpResponse, bufferedStream, cancellationToken).ConfigureAwait(false);

            return httpResponse;
        }

        private static async Task SetHeadersAndContentAsync(
            HttpResponseMessage httpResponse,
            HttpBufferedStream bufferedStream,
            CancellationToken cancellationToken)
        {
            IList<string> headers = new List<string>();
            string line = await bufferedStream.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            while (!string.IsNullOrWhiteSpace(line))
            {
                headers.Add(line);
                line = await bufferedStream.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            }

            httpResponse.Content = new StreamContent(bufferedStream);
            foreach (string header in headers)
            {
                if (string.IsNullOrWhiteSpace(header))
                {
                    // headers end
                    break;
                }

                int headerSeparatorPosition = header.IndexOf(
                    HeaderSeparator.ToString(CultureInfo.InvariantCulture),
                    StringComparison.InvariantCultureIgnoreCase);

                if (headerSeparatorPosition <= 0)
                {
                    throw new IotHubClientException($"Header is invalid {header}.")
                    {
                        ErrorCode = IotHubClientErrorCode.NetworkErrors,
                    };
                }

                string headerName = header.Substring(0, headerSeparatorPosition).Trim();
                string headerValue = header.Substring(headerSeparatorPosition + 1).Trim();

                bool headerAdded = httpResponse.Headers.TryAddWithoutValidation(headerName, headerValue);
                if (!headerAdded)
                {
                    if (string.Equals(headerName, ContentLengthHeaderName, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!long.TryParse(headerValue, out long contentLength))
                        {
                            throw new IotHubClientException($"Header value is invalid for {headerName}.")
                            {
                                ErrorCode = IotHubClientErrorCode.NetworkErrors,
                            };
                        }

                        try
                        {
                            await httpResponse.Content.LoadIntoBufferAsync(contentLength).ConfigureAwait(false);
                        }
                        catch (HttpRequestException ex)
                        {
                            throw new IotHubClientException(ex.Message, ex)
                            {
                                ErrorCode = IotHubClientErrorCode.NetworkErrors,
                            };
                        }
                    }

                    httpResponse.Content.Headers.TryAddWithoutValidation(headerName, headerValue);
                }
            }
        }

        private static async Task SetResponseStatusLineAsync(HttpResponseMessage httpResponse, HttpBufferedStream bufferedStream, CancellationToken cancellationToken)
        {
            string statusLine = await bufferedStream.ReadLineAsync(cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrWhiteSpace(statusLine))
            {
                throw new IotHubClientException("Response is empty.")
                {
                    ErrorCode = IotHubClientErrorCode.NetworkErrors,
                };
            }

            string[] statusLineParts = statusLine.Split(new[] { Space }, 3);
            if (statusLineParts.Length < 3)
            {
                throw new IotHubClientException("Status line is not valid.")
                {
                    ErrorCode = IotHubClientErrorCode.NetworkErrors,
                };
            }

            string[] httpVersion = statusLineParts[0].Split(new[] { ProtocolVersionSeparator }, 2);
            if (httpVersion.Length < 2 || !Version.TryParse(httpVersion[1], out Version versionNumber))
            {
                throw new IotHubClientException($"Version is not valid {statusLineParts[0]}.")
                {
                    ErrorCode = IotHubClientErrorCode.NetworkErrors,
                };
            }

            httpResponse.Version = versionNumber;

            if (!Enum.TryParse(statusLineParts[1], out HttpStatusCode statusCode))
            {
                throw new IotHubClientException($"StatusCode is not valid {statusLineParts[1]}.")
                {
                    ErrorCode = IotHubClientErrorCode.NetworkErrors,
                };
            }

            httpResponse.StatusCode = statusCode;
            httpResponse.ReasonPhrase = statusLineParts[2];
        }

        private static void PreProcessRequest(HttpRequestMessage request)
        {
            if (string.IsNullOrEmpty(request.Headers.Host))
            {
                request.Headers.Host = $"{request.RequestUri.DnsSafeHost}:{request.RequestUri.Port}";
            }

            request.Headers.ConnectionClose = true;
        }
    }
}
