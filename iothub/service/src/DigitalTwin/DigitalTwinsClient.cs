// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Http2;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// This client contains methods to retrieve and update digital twin information, and invoke commands
    /// on a digital twin device.
    /// </summary>
    /// <seealso href="https://docs.microsoft.com/azure/iot-develop/concepts-digital-twin"/>
    public class DigitalTwinsClient
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;

        private const string DigitalTwinRequestUriFormat = "/digitaltwins/{0}";
        private const string DigitalTwinCommandRequestUriFormat = "/digitaltwins/{0}/commands/{1}";
        private const string DigitalTwinComponentCommandRequestUriFormat = "/digitaltwins/{0}/components/{1}/commands/{2}";

        // HttpMethod does not define PATCH in its enum in .netstandard 2.0, so this is the only way to create an
        // HTTP patch request.
        private readonly HttpMethod _patch = new HttpMethod("PATCH");

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected DigitalTwinsClient()
        {
        }

        internal DigitalTwinsClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _hostName = hostName;
            _credentialProvider = credentialProvider;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <summary>
        /// Gets a strongly-typed digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The deserialized application/json digital twin and the ETag for the digital twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided digital twin id is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided digital twin id is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<GetDigitalTwinResponse<T>> GetAsync<T>(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting digital twin with Id: {digitalTwinId}", nameof(GetAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(digitalTwinId, nameof(digitalTwinId));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(HttpMethod.Get, GetDigitalTwinRequestUri(digitalTwinId), _credentialProvider);
                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response).ConfigureAwait(false);
                T digitalTwin = await HttpMessageHelper2.DeserializeResponse<T>(response, cancellationToken).ConfigureAwait(false);
                string etag = response.Headers.GetValues("ETag").FirstOrDefault();
                return new GetDigitalTwinResponse<T>(digitalTwin, etag);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetAsync)} threw an exception: {ex}", nameof(GetAsync));
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
        /// <param name="digitalTwinUpdateOperations">The application/json-patch+json operations to be performed on the specified digital twin.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The new ETag for the digital twin and the URI location of the digital twin.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided digital twin id or digitalTwinUpdateOperations is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided digital twin id or digitalTwinUpdateOperations is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<UpdateDigitalTwinResponse> UpdateAsync(
            string digitalTwinId,
            string digitalTwinUpdateOperations,
            UpdateDigitalTwinOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating digital twin with Id: {digitalTwinId}", nameof(UpdateAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(digitalTwinId, nameof(digitalTwinId));
                Argument.RequireNotNullOrEmpty(digitalTwinUpdateOperations, nameof(digitalTwinUpdateOperations));

                cancellationToken.ThrowIfCancellationRequested();

                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    _patch,
                    GetDigitalTwinRequestUri(digitalTwinId),
                    _credentialProvider,
                    digitalTwinUpdateOperations);

                if (!string.IsNullOrWhiteSpace(requestOptions?.IfMatch))
                {
                    HttpMessageHelper2.InsertEtag(request, requestOptions?.IfMatch);
                }

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.Accepted, response).ConfigureAwait(false);

                var updateResponse = new UpdateDigitalTwinResponse()
                {
                    ETag = response.Headers.GetValues("ETag").FirstOrDefault(),
                    Location = response.Headers.GetValues("Location").FirstOrDefault()
                };

                return updateResponse;
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(UpdateAsync)} threw an exception: {ex}", nameof(UpdateAsync));
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
        /// <exception cref="ArgumentNullException">Thrown when the provided digital twin id or command name is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided digital twin id or command name is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<InvokeDigitalTwinCommandResponse> InvokeCommandAsync(
            string digitalTwinId,
            string commandName,
            InvokeDigitalTwinCommandOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking command on digital twin with Id: {digitalTwinId}", nameof(InvokeCommandAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(digitalTwinId, nameof(digitalTwinId));
                Argument.RequireNotNullOrEmpty(commandName, nameof(commandName));

                cancellationToken.ThrowIfCancellationRequested();

                string queryStringParameters = BuildCommandRequestQueryStringParameters(requestOptions);
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Post,
                    GetDigitalTwinCommandRequestUri(digitalTwinId, commandName),
                    _credentialProvider,
                    requestOptions?.Payload,
                    queryStringParameters);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response).ConfigureAwait(false);

                // No need to deserialize here since the user will deserialize this into their expected type
                // after this function returns.
                string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int responseStatusCode = int.Parse(response.Headers.GetValues("x-ms-command-statuscode").FirstOrDefault());
                string requestId = response.Headers.GetValues("x-ms-request-id").FirstOrDefault();
                var commandResponse = new InvokeDigitalTwinCommandResponse()
                {
                    Payload = responsePayload,
                    Status = responseStatusCode,
                    RequestId = requestId,
                };

                return commandResponse;
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(InvokeCommandAsync)} threw an exception: {ex}", nameof(InvokeCommandAsync));
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
        /// <exception cref="ArgumentNullException">Thrown when the provided digital twin id or command name or component name is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided digital twin id or command name or component name is empty or whitespace.</exception>
        /// <exception cref="IotHubException">
        /// Thrown if IoT hub responded to the request with a non-successful status code. For example, if the provided
        /// request was throttled, <see cref="IotHubThrottledException"/> is thrown. For a complete list of possible
        /// error cases, see <see cref="Common.Exceptions"/>.
        /// </exception>
        /// <exception cref="HttpRequestException">
        /// If the HTTP request fails due to an underlying issue such as network connectivity, DNS failure, or server
        /// certificate validation.
        /// </exception>
        /// <exception cref="OperationCanceledException">If the provided cancellation token has requested cancellation.</exception>
        public virtual async Task<InvokeDigitalTwinCommandResponse> InvokeComponentCommandAsync(
            string digitalTwinId,
            string componentName,
            string commandName,
            InvokeDigitalTwinCommandOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Invoking component command on digital twin with Id: {digitalTwinId}", nameof(InvokeComponentCommandAsync));

            try
            {
                Argument.RequireNotNullOrEmpty(digitalTwinId, nameof(digitalTwinId));
                Argument.RequireNotNullOrEmpty(componentName, nameof(componentName));
                Argument.RequireNotNullOrEmpty(commandName, nameof(commandName));

                cancellationToken.ThrowIfCancellationRequested();

                string queryStringParameters = BuildCommandRequestQueryStringParameters(requestOptions);
                using HttpRequestMessage request = _httpRequestMessageFactory.CreateRequest(
                    HttpMethod.Post,
                    GetDigitalTwinComponentCommandRequestUri(digitalTwinId, componentName, commandName),
                    _credentialProvider,
                    requestOptions?.Payload,
                    queryStringParameters);

                HttpResponseMessage response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
                await HttpMessageHelper2.ValidateHttpResponseStatus(HttpStatusCode.OK, response).ConfigureAwait(false);

                // No need to deserialize here since the user will deserialize this into their expected type
                // after this function returns.
                string responsePayload = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                int responseStatusCode = int.Parse(response.Headers.GetValues("x-ms-command-statuscode").FirstOrDefault());
                string requestId = response.Headers.GetValues("x-ms-request-id").FirstOrDefault();
                var commandResponse = new InvokeDigitalTwinCommandResponse()
                {
                    Payload = responsePayload,
                    Status = responseStatusCode,
                    RequestId = requestId,
                };

                return commandResponse;
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(InvokeComponentCommandAsync)} threw an exception: {ex}", nameof(InvokeComponentCommandAsync));
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
            return new Uri(DigitalTwinRequestUriFormat.FormatInvariant(digitalTwinId), UriKind.Relative);
        }

        private static Uri GetDigitalTwinCommandRequestUri(string digitalTwinId, string commandName)
        {
            digitalTwinId = WebUtility.UrlEncode(digitalTwinId);
            commandName = WebUtility.UrlEncode(commandName);
            return new Uri(DigitalTwinCommandRequestUriFormat.FormatInvariant(digitalTwinId, commandName), UriKind.Relative);
        }

        private static Uri GetDigitalTwinComponentCommandRequestUri(string digitalTwinId, string componentPath, string commandName)
        {
            digitalTwinId = WebUtility.UrlEncode(digitalTwinId);
            componentPath = WebUtility.UrlEncode(componentPath);
            commandName = WebUtility.UrlEncode(commandName);
            return new Uri(DigitalTwinComponentCommandRequestUriFormat.FormatInvariant(digitalTwinId, componentPath, commandName), UriKind.Relative);
        }

        // Root level commands and component level commands append the connect and read timeout values as query string values such as:
        // /digitaltwins/myDigitalTwin/commands/myCommand?api-version="2021-04-12"&connectTimeoutInSeconds=10&readTimeoutInSeconds=20
        private static string BuildCommandRequestQueryStringParameters(InvokeDigitalTwinCommandOptions requestOptions)
        {
            string queryStringParameters = "";
            if (requestOptions?.ConnectTimeoutInSeconds != null)
            {
                queryStringParameters += string.Format($"&connectTimeoutInSeconds={requestOptions.ConnectTimeoutInSeconds}");
            }

            if (requestOptions?.ResponseTimeoutInSeconds != null)
            {
                queryStringParameters += string.Format($"&responseTimeoutInSeconds={requestOptions.ResponseTimeoutInSeconds}");
            }

            return queryStringParameters;
        }
    }
}
