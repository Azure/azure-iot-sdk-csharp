// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// This client contains methods to retrieve and update digital twin information, and invoke commands
    /// on a digital twin device.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-develop/concepts-digital-twin"/>
    public class DigitalTwinsClient
    {
        private const string DigitalTwinRequestUriFormat = "/digitaltwins/{0}";
        private const string DigitalTwinCommandRequestUriFormat = "/digitaltwins/{0}/commands/{1}";
        private const string DigitalTwinComponentCommandRequestUriFormat = "/digitaltwins/{0}/components/{1}/commands/{2}";
        private const string StatusCodeHeaderKey = "x-ms-command-statuscode";
        private const string RequestIdHeaderKey = "x-ms-request-id";

        // HttpMethod does not define PATCH in its enum in .netstandard 2.0, so this is the only way to create an
        // HTTP patch request.
        private static readonly HttpMethod s_patch = new("PATCH");

        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly RetryHandler _internalRetryHandler;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DigitalTwinsClient()
        {
        }

        internal DigitalTwinsClient(
            string hostName,
            IotHubConnectionProperties credentialProvider,
            HttpClient httpClient,
            HttpRequestMessageFactory httpRequestMessageFactory,
            RetryHandler retryHandler)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
            _internalRetryHandler = retryHandler;
        }

        /// <summary>
        /// Gets a strongly-typed digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized application/json digital twin and the ETag for the digital twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="digitalTwinId"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="digitalTwinId"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<DigitalTwinGetResponse<T>> GetAsync<T>(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting digital twin with Id: {digitalTwinId}", nameof(GetAsync));

            Argument.AssertNotNullOrWhiteSpace(digitalTwinId, nameof(digitalTwinId));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetDigitalTwinRequestUri(digitalTwinId), _credentialProvider);
                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);
                T digitalTwin = await HttpMessageHelper.DeserializeResponseAsync<T>(response).ConfigureAwait(false);
                var etag = new ETag(response.Headers.GetValues("ETag").FirstOrDefault());
                return new DigitalTwinGetResponse<T>(digitalTwin, etag);
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Getting digital twin with Id {digitalTwinId} threw an exception: {ex}", nameof(GetAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting digital twin with Id: {digitalTwinId}", nameof(GetAsync));
            }
        }

        /// <summary>
        /// Updates a digital twin.
        /// </summary>
        /// <remarks>
        /// For further information on how to create the json-patch, see <see href="https://docs.microsoft.com/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </remarks>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="jsonPatch">
        /// The application/json-patch+json operations to be performed on the specified digital twin.
        /// This patch can be constructed using <see cref="JsonPatchDocument"/>. See the example code for more details.
        /// </param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The new ETag for the digital twin and the URI location of the digital twin.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="digitalTwinId"/> or <paramref name="jsonPatch"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="digitalTwinId"/> or <paramref name="jsonPatch"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        /// <example>
        /// <code language="csharp">
        /// string propertyName = "targetTemperature";
        /// int propertyValue = 12;
        /// var propertyValues = new Dictionary&lt;string, object&gt; { { propertyName, propertyValue } };
        /// var patchDocument = new JsonPatchDocument();
        /// patchDocument.AppendAdd("/myComponentName", propertyValues);
        /// string jsonPatch = patchDocument.ToString();
        /// DigitalTwinUpdateResponse updateResponse = await serviceClient.DigitalTwins.UpdateAsync(deviceId, jsonPatch);
        /// </code>
        /// </example>
        public virtual async Task<DigitalTwinUpdateResponse> UpdateAsync(
            string digitalTwinId,
            string jsonPatch,
            UpdateDigitalTwinOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating digital twin with Id: {digitalTwinId}", nameof(UpdateAsync));

            Argument.AssertNotNullOrWhiteSpace(digitalTwinId, nameof(digitalTwinId));
            Argument.AssertNotNullOrWhiteSpace(jsonPatch, nameof(jsonPatch));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    s_patch,
                    GetDigitalTwinRequestUri(digitalTwinId),
                    _credentialProvider,
                    jsonPatch);

                if (!string.IsNullOrWhiteSpace(requestOptions?.IfMatch.ToString()))
                {
                    HttpMessageHelper.ConditionallyInsertETag(request, requestOptions.IfMatch, false);
                }

                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.Accepted, response).ConfigureAwait(false);

                var updateResponse = new DigitalTwinUpdateResponse(
                    new ETag(response.Headers.GetValues("ETag").FirstOrDefault()),
                    response.Headers.GetValues("Location").FirstOrDefault());

                return updateResponse;
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Updating digital twin with Id {digitalTwinId} threw an exception: {ex}", nameof(UpdateAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating digital twin with Id: {digitalTwinId}", nameof(UpdateAsync));
            }
        }

        /// <summary>
        /// Invoke a command on a digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="commandName">The command to be invoked.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The serialized application/json command invocation response, the command response status code, and the request id.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="digitalTwinId"/> or <paramref name="commandName"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="digitalTwinId"/> or <paramref name="commandName"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<InvokeDigitalTwinCommandResponse> InvokeCommandAsync(
            string digitalTwinId,
            string commandName,
            InvokeDigitalTwinCommandOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking command on digital twin with Id: {digitalTwinId}", nameof(InvokeCommandAsync));

            Argument.AssertNotNullOrWhiteSpace(digitalTwinId, nameof(digitalTwinId));
            Argument.AssertNotNullOrWhiteSpace(commandName, nameof(commandName));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string queryStringParameters = BuildCommandRequestQueryStringParameters(requestOptions);
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Post,
                    GetDigitalTwinCommandRequestUri(digitalTwinId, commandName),
                    _credentialProvider,
                    requestOptions?.Payload,
                    queryStringParameters);

                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

                // No need to deserialize here since the user will deserialize this into their expected type
                // after this function returns.
                string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int responseStatusCode = int.Parse(response.Headers.GetValues(StatusCodeHeaderKey).FirstOrDefault(), CultureInfo.CurrentCulture);
                string requestId = response.Headers.GetValues(RequestIdHeaderKey).FirstOrDefault();
                return new InvokeDigitalTwinCommandResponse
                {
                    Payload = responsePayload,
                    Status = responseStatusCode,
                    RequestId = requestId,
                };
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Invoking command on digital twin with Id {digitalTwinId} threw an exception: {ex}", nameof(InvokeCommandAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Invoking command on digital twin with Id: {digitalTwinId}", nameof(InvokeCommandAsync));
            }
        }

        /// <summary>
        /// Invoke a command on a component of a digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="componentName">The component name under which the command is defined.</param>
        /// <param name="commandName">The command to be invoked.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The serialized application/json command invocation response, the command response status code, and the request id.</returns>
        /// <exception cref="ArgumentNullException">When the provided <paramref name="digitalTwinId"/> or <paramref name="componentName"/> or <paramref name="commandName"/> is null.</exception>
        /// <exception cref="ArgumentException">When the provided <paramref name="digitalTwinId"/> or <paramref name="componentName"/> or <paramref name="commandName"/> is empty or whitespace.</exception>
        /// <exception cref="IotHubServiceException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubServiceException"/> with <see cref="IotHubServiceErrorCode.ThrottlingException"/> is thrown.
        /// For a complete list of possible error cases, see <see cref="IotHubServiceErrorCode"/>.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided <paramref name="cancellationToken"/> has requested cancellation.</exception>
        public virtual async Task<InvokeDigitalTwinCommandResponse> InvokeComponentCommandAsync(
            string digitalTwinId,
            string componentName,
            string commandName,
            InvokeDigitalTwinCommandOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking component command on digital twin with Id: {digitalTwinId}", nameof(InvokeComponentCommandAsync));

            Argument.AssertNotNullOrWhiteSpace(digitalTwinId, nameof(digitalTwinId));
            Argument.AssertNotNullOrWhiteSpace(componentName, nameof(componentName));
            Argument.AssertNotNullOrWhiteSpace(commandName, nameof(commandName));

            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                string queryStringParameters = BuildCommandRequestQueryStringParameters(requestOptions);
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Post,
                    GetDigitalTwinComponentCommandRequestUri(digitalTwinId, componentName, commandName),
                    _credentialProvider,
                    requestOptions?.Payload,
                    queryStringParameters);

                HttpResponseMessage response = null;

                await _internalRetryHandler
                    .RunWithRetryAsync(
                        async () =>
                        {
                            response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                        },
                        cancellationToken)
                    .ConfigureAwait(false);

                await HttpMessageHelper.ValidateHttpResponseStatusAsync(HttpStatusCode.OK, response).ConfigureAwait(false);

                // No need to deserialize here since the user will deserialize this into their expected type
                // after this function returns.
                string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int responseStatusCode = int.Parse(response.Headers.GetValues(StatusCodeHeaderKey).FirstOrDefault(), CultureInfo.CurrentCulture);
                string requestId = response.Headers.GetValues(RequestIdHeaderKey).FirstOrDefault();
                return new InvokeDigitalTwinCommandResponse
                {
                    Payload = responsePayload,
                    Status = responseStatusCode,
                    RequestId = requestId,
                };
            }
            catch (HttpRequestException ex)
            {
                if (Fx.ContainsAuthenticationException(ex))
                {
                    throw new IotHubServiceException(ex.Message, HttpStatusCode.Unauthorized, IotHubServiceErrorCode.IotHubUnauthorizedAccess, null, ex);
                }
                throw new IotHubServiceException(ex.Message, HttpStatusCode.RequestTimeout, IotHubServiceErrorCode.Unknown, null, ex);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"Invoking component command on digital twin with Id {digitalTwinId} threw an exception: {ex}", nameof(InvokeComponentCommandAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Invoking component command on digital twin with Id: {digitalTwinId}", nameof(InvokeComponentCommandAsync));
            }
        }

        private static Uri GetDigitalTwinRequestUri(string digitalTwinId)
        {
            digitalTwinId = WebUtility.UrlEncode(digitalTwinId);
            return new Uri(string.Format(CultureInfo.InvariantCulture, DigitalTwinRequestUriFormat, digitalTwinId), UriKind.Relative);
        }

        private static Uri GetDigitalTwinCommandRequestUri(string digitalTwinId, string commandName)
        {
            digitalTwinId = WebUtility.UrlEncode(digitalTwinId);
            commandName = WebUtility.UrlEncode(commandName);

            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DigitalTwinCommandRequestUriFormat,
                    digitalTwinId,
                    commandName),
                UriKind.Relative);
        }

        private static Uri GetDigitalTwinComponentCommandRequestUri(string digitalTwinId, string componentPath, string commandName)
        {
            digitalTwinId = WebUtility.UrlEncode(digitalTwinId);
            componentPath = WebUtility.UrlEncode(componentPath);
            commandName = WebUtility.UrlEncode(commandName);

            return new Uri(
                string.Format(
                    CultureInfo.InvariantCulture,
                    DigitalTwinComponentCommandRequestUriFormat,
                    digitalTwinId,
                    componentPath,
                    commandName),
                UriKind.Relative);
        }

        // Root level commands and component level commands append the connect and read timeout values as query string values such as:
        // /digitaltwins/myDigitalTwin/commands/myCommand?api-version="2021-04-12"&connectTimeoutInSeconds=10&readTimeoutInSeconds=20
        private static string BuildCommandRequestQueryStringParameters(InvokeDigitalTwinCommandOptions requestOptions)
        {
            string queryStringParameters = "";

            if (requestOptions?.ConnectTimeout != null)
            {
                queryStringParameters += $"&connectTimeoutInSeconds={(int)requestOptions.ConnectTimeout.Value.TotalSeconds}";
            }

            if (requestOptions?.ResponseTimeout != null)
            {
                queryStringParameters += $"&responseTimeoutInSeconds={(int)requestOptions.ResponseTimeout.Value.TotalSeconds}";
            }

            return queryStringParameters;
        }
    }
}
