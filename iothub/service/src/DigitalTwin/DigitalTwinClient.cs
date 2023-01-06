// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Azure.Devices.Extensions;
using Microsoft.Azure.Devices.Generated;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Rest;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The Digital Twins Service Client contains methods to retrieve and update digital twin information, and invoke commands on a digital twin device.
    /// </summary>
    public class DigitalTwinClient : IDisposable
    {
        private readonly IotHubGatewayServiceAPIs _client;
        private readonly DigitalTwin _protocolLayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinClient"/> class.</summary>
        /// <param name="connectionString">The IoT hub's connection string.</param>
        /// <param name="handlers">The delegating handlers to add to the http client pipeline. You can add handlers for tracing, implementing a retry strategy, routing requests through a proxy, etc.</param>
        public static DigitalTwinClient CreateFromConnectionString(string connectionString, params DelegatingHandler[] handlers)
        {
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            var sharedAccessKeyCredential = new SharedAccessKeyCredentials(connectionString);
            return new DigitalTwinClient(iotHubConnectionString.HttpsEndpoint, sharedAccessKeyCredential, handlers);
        }

        private DigitalTwinClient(Uri uri, IotServiceClientCredentials credentials, params DelegatingHandler[] handlers)
        {
            _client = new IotHubGatewayServiceAPIs(uri, credentials, handlers);
            _protocolLayer = new DigitalTwin(_client);

            JsonConvert.DefaultSettings = JsonSerializerSettingsInitializer.GetDefaultJsonSerializerSettingsDelegate();
        }

        /// <summary>
        /// Gets a strongly-typed digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The application/json digital twin and the http response.</returns>
        public async Task<HttpOperationResponse<T, DigitalTwinGetHeaders>> GetDigitalTwinAsync<T>(string digitalTwinId, CancellationToken cancellationToken = default)
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
        /// <para>For further information on how to create the json-patch, see <see href="https://docs.microsoft.com/en-us/azure/iot-pnp/howto-manage-digital-twin."/></para>
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="digitalTwinUpdateOperations">The application/json-patch+json operations to be performed on the specified digital twin.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The http response.</returns>
        public Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>> UpdateDigitalTwinAsync(
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
        public async Task<HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders>> InvokeCommandAsync(
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
        /// <returns>The application/json command invocation response and the http response. </returns>
        public async Task<HttpOperationResponse<DigitalTwinCommandResponse, DigitalTwinInvokeCommandHeaders>> InvokeComponentCommandAsync(
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
    }
}
