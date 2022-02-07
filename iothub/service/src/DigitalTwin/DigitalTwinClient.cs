// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.DigitalTwin.Authentication;
using Microsoft.Azure.Devices.Extensions;
using Microsoft.Azure.Devices.Generated;
using Microsoft.Rest;
using Newtonsoft.Json;
using PnpDigitalTwin = Microsoft.Azure.Devices.Generated.DigitalTwin;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The Digital Twins Service Client contains methods to retrieve and update digital twin information, and invoke commands on a digital twin device.
    /// </summary>
    /// <remarks>
    /// For more information, see <see href="https://github.com/Azure/azure-iot-sdk-csharp#iot-hub-service-sdk"/>
    /// </remarks>
    public class DigitalTwinClient : IDisposable
    {
        private const string HttpsEndpointPrefix = "https";
        private readonly IotHubGatewayServiceAPIs _client;
        private readonly PnpDigitalTwin _protocolLayer;

        /// <summary>
        /// Creates an instance of <see cref="DigitalTwinClient"/>, provided for unit testing purposes only.
        /// </summary>
        public DigitalTwinClient()
        {
        }

        /// <summary>
        /// Creates DigitalTwinClient from an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT hub's connection string.</param>
        /// <param name="handlers">
        /// The delegating handlers to add to the http client pipeline.
        /// You can add handlers for tracing, implementing a retry strategy, routing requests through a proxy, etc.
        /// </param>
        /// <returns>A DigitalTwinsClient instance.</returns>
        public static DigitalTwinClient CreateFromConnectionString(string connectionString, params DelegatingHandler[] handlers)
        {
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            var connectionStringCredential = new DigitalTwinConnectionStringCredential(iotHubConnectionString);
            return new DigitalTwinClient(iotHubConnectionString.HostName, connectionStringCredential, handlers);
        }

        /// <summary>
        /// Creates DigitalTwinClient, authenticating using an identity in Azure Active Directory (AAD).
        /// </summary>
        /// <remarks>
        /// For more about information on the options of authenticating using a derived instance of <see cref="TokenCredential"/>, see
        /// <see href="https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme"/>.
        /// For more information on configuring IoT hub with Azure Active Directory, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Azure Active Directory (AAD) credentials to authenticate with IoT hub. See <see cref="TokenCredential"/></param>
        /// <param name="handlers">
        /// The delegating handlers to add to the http client pipeline. You can add handlers for tracing,
        /// implementing a retry strategy, routing requests through a proxy, etc.
        /// </param>
        /// <returns>A DigitalTwinsClient instance.</returns>
        public static DigitalTwinClient Create(
            string hostName,
            TokenCredential credential,
            params DelegatingHandler[] handlers)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var tokenCredential = new DigitalTwinTokenCredential(credential);
            return new DigitalTwinClient(hostName, tokenCredential, handlers);
        }

        /// <summary>
        /// Creates DigitalTwinClient using a shared access signature provided and refreshed as necessary by the caller.
        /// </summary>
        /// <remarks>
        /// Users may wish to build their own shared access signature (SAS) tokens rather than give the shared key to the SDK and let it manage signing and renewal.
        /// The <see cref="AzureSasCredential"/> object gives the SDK access to the SAS token, while the caller can update it as necessary using the
        /// <see cref="AzureSasCredential.Update(string)"/> method.
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="handlers">The delegating handlers to add to the http client pipeline. You can add handlers for tracing, implementing a retry strategy, routing requests through a proxy, etc.</param>
        /// <returns>A DigitalTwinsClient instance.</returns>
        public static DigitalTwinClient Create(
            string hostName,
            AzureSasCredential credential,
            params DelegatingHandler[] handlers)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var sasCredential = new DigitalTwinSasCredential(credential);
            return new DigitalTwinClient(hostName, sasCredential, handlers);
        }

        /// <summary>
        /// Gets a strongly-typed digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The application/json digital twin and the http response.</returns>
        public virtual async Task<HttpOperationResponse<T, DigitalTwinGetHeaders>> GetDigitalTwinAsync<T>(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            using HttpOperationResponse<string, DigitalTwinGetHeaders> response = await _protocolLayer.GetDigitalTwinWithHttpMessagesAsync(digitalTwinId, null, cancellationToken)
                .ConfigureAwait(false);
            return new HttpOperationResponse<T, DigitalTwinGetHeaders>
            {
                Body = typeof(T) == typeof(string) ? (T)(object)response.Body : JsonConvert.DeserializeObject<T>(response.Body),
                Headers = response.Headers,
                Request = response.Request,
                Response = response.Response
            };
        }

        /// <summary>
        /// Updates a digital twin.
        /// </summary>
        /// <remarks>
        /// For further information on how to create the json-patch, see <see href="https://docs.microsoft.com/en-us/azure/iot-pnp/howto-manage-digital-twin"/>.
        /// </remarks>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="digitalTwinUpdateOperations">The application/json-patch+json operations to be performed on the specified digital twin.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The http response.</returns>
        public virtual Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>> UpdateDigitalTwinAsync(
            string digitalTwinId,
            string digitalTwinUpdateOperations,
            DigitalTwinUpdateRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            return _protocolLayer.UpdateDigitalTwinWithHttpMessagesAsync(digitalTwinId, digitalTwinUpdateOperations, requestOptions?.IfMatch, null, cancellationToken);
        }

        /// <summary>
        /// Invoke a command on a digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="commandName">The command to be invoked.</param>
        /// <param name="payload">The command payload.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The application/json command invocation response and the http response. </returns>
        public virtual async Task<HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders>> InvokeCommandAsync(
            string digitalTwinId,
            string commandName,
            string payload = default,
            DigitalTwinInvokeCommandRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            using HttpOperationResponse<string, DigitalTwinInvokeRootLevelCommandHeaders> response = await _protocolLayer.InvokeRootLevelCommandWithHttpMessagesAsync(
                digitalTwinId,
                commandName,
                payload,
                requestOptions?.ConnectTimeoutInSeconds,
                requestOptions?.ResponseTimeoutInSeconds,
                null,
                cancellationToken)
                .ConfigureAwait(false);
            return new HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders>
            {
                Body = new DigitalTwinCommandResponse { Status = response.Headers.XMsCommandStatuscode, Payload = response.Body },
                Headers = new DigitalTwinInvokeCommandHeaders { RequestId = response.Headers.XMsRequestId },
                Request = response.Request,
                Response = response.Response,
            };
        }

        /// <summary>
        /// Invoke a command on a component of a digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="componentName">The component name under which the command is defined.</param>
        /// <param name="commandName">The command to be invoked.</param>
        /// <param name="payload">The command payload.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The application/json command invocation response and the http response.</returns>
        public virtual async Task<HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders>> InvokeComponentCommandAsync(
            string digitalTwinId,
            string componentName,
            string commandName,
            string payload = default,
            DigitalTwinInvokeCommandRequestOptions requestOptions = default,
            CancellationToken cancellationToken = default)
        {
            using HttpOperationResponse<string, DigitalTwinInvokeComponentCommandHeaders> response = await _protocolLayer.InvokeComponentCommandWithHttpMessagesAsync(
                digitalTwinId,
                componentName,
                commandName,
                payload,
                requestOptions?.ConnectTimeoutInSeconds,
                requestOptions?.ResponseTimeoutInSeconds,
                null,
                cancellationToken)
                .ConfigureAwait(false);
            return new HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders>
            {
                Body = new DigitalTwinCommandResponse { Status = response.Headers.XMsCommandStatuscode, Payload = response.Body },
                Headers = new DigitalTwinInvokeCommandHeaders { RequestId = response.Headers.XMsRequestId },
                Request = response.Request,
                Response = response.Response,
            };
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            _client?.Dispose();
        }

        private DigitalTwinClient(string hostName, DigitalTwinServiceClientCredentials credentials, params DelegatingHandler[] handlers)
        {
            Uri httpsEndpoint = new UriBuilder(HttpsEndpointPrefix, hostName).Uri;
            HttpMessageHandler httpMessageHandler = HttpClientHelper.CreateDefaultHttpMessageHandler(null, httpsEndpoint, ServicePointHelpers.DefaultConnectionLeaseTimeout);
#pragma warning disable CA2000 // Dispose objects before losing scope (httpMessageHandlerWithDelegatingHandlers is disposed when the http client owning it is disposed)
            HttpMessageHandler httpMessageHandlerWithDelegatingHandlers = CreateHttpHandlerPipeline(httpMessageHandler, handlers);
#pragma warning restore CA2000 // Dispose objects before losing scope

#pragma warning disable CA2000 // Dispose objects before losing scope (httpClient is disposed when the protocol layer client owning it is disposed)
            var httpClient = new HttpClient(httpMessageHandlerWithDelegatingHandlers, true)
            {
                BaseAddress = httpsEndpoint
            };
#pragma warning restore CA2000 // Dispose objects before losing scope

#pragma warning restore CA2000 // Dispose objects before losing scope

            // When this client is disposed, all the http message handlers and delegating handlers will be disposed automatically
            _client = new IotHubGatewayServiceAPIs(credentials, httpClient, true);
            _client.BaseUri = httpsEndpoint;
            _protocolLayer = new PnpDigitalTwin(_client);
        }

        // Creates a single HttpMessageHandler to construct a HttpClient with from a base httpMessageHandler and some number of custom delegating handlers
        // This is almost a copy of the Microsoft.Rest.ClientRuntime library's implementation, but with the return and parameter type HttpClientHandler replaced
        // with the more abstract HttpMessageHandler in order for us to set the base handler as either a SocketsHttpHandler for .net core or an HttpClientHandler otherwise
        // https://github.com/Azure/azure-sdk-for-net/blob/99f4da88ab0aa01c79aa291c6c101ab94c4ac940/sdk/mgmtcommon/ClientRuntime/ClientRuntime/ServiceClient.cs#L376
        private static HttpMessageHandler CreateHttpHandlerPipeline(HttpMessageHandler httpMessageHandler, params DelegatingHandler[] handlers)
        {
            // The RetryAfterDelegatingHandler should be the absolute outermost handler
            // because it's extremely lightweight and non-interfering
            HttpMessageHandler currentHandler =
#pragma warning disable CA2000 // Dispose objects before losing scope (delegating handler is disposed when the http client that uses it is disposed)
                new RetryDelegatingHandler(new RetryAfterDelegatingHandler { InnerHandler = httpMessageHandler });
#pragma warning restore CA2000 // Dispose objects before losing scope

            if (handlers != null)
            {
                for (int i = handlers.Length - 1; i >= 0; --i)
                {
                    DelegatingHandler handler = handlers[i];
                    // Non-delegating handlers are ignored since we always
                    // have RetryDelegatingHandler as the outer-most handler
                    while (handler.InnerHandler is DelegatingHandler)
                    {
                        handler = handler.InnerHandler as DelegatingHandler;
                    }

                    handler.InnerHandler = currentHandler;
                    currentHandler = handlers[i];
                }
            }

            return currentHandler;
        }
    }
}
