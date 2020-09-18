// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Generated;
using Microsoft.Azure.Devices.Generated.Models;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Rest;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices
{
    public class DigitalTwinClient : IDisposable
    {
        private readonly IotHubGatewayServiceAPIs _client;
        private readonly DigitalTwin _protocolLayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinClient"/> class.</summary>
        /// <param name="connectionString">Your IoT hub's connection string.</param>
        public static DigitalTwinClient CreateFromConnectionString(string connectionString)
        {
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            var sharedAccessKeyCredential = new SharedAccessKeyCredentials(connectionString);
            return new DigitalTwinClient(iotHubConnectionString.HttpsEndpoint, sharedAccessKeyCredential);
        }

        /// <summary> Initializes a new instance of the <see cref="DigitalTwinClient"/> class.</summary>
        protected DigitalTwinClient()
        {
            // for mocking purposes only
        }

        private DigitalTwinClient(Uri uri, IotServiceClientCredentials credentials)
        {
            _client = new IotHubGatewayServiceAPIs(uri, credentials);
            _protocolLayer = new DigitalTwin(_client);
        }

        /// <summary>
        /// Gets a strongly-typed digital twin asynchronously.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The application/json digital twin and the http response.</returns>
        public async Task<HttpOperationResponse<T, DigitalTwinGetHeaders>> GetAsync<T>(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            using HttpOperationResponse<string, DigitalTwinGetHeaders> response = await _protocolLayer.GetDigitalTwinWithHttpMessagesAsync(digitalTwinId, null, cancellationToken)
                .ConfigureAwait(false);
            return new HttpOperationResponse<T, DigitalTwinGetHeaders>
            {
                Body = JsonConvert.DeserializeObject<T>(response.Body),
                Headers = response.Headers,
                Request = response.Request,
                Response = response.Response
            };
        }

        /// <summary>
        /// Updates a digital twin asynchronously.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="digitalTwinUpdateOperations">The application/json-patch+json operations to be performed on the specified digital twin.</param>
        /// <param name="requestOptions">The optional settings for this request.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The http response.</returns>
        public Task<HttpOperationHeaderResponse<DigitalTwinUpdateHeaders>> UpdateAsync(string digitalTwinId, string digitalTwinUpdateOperations, DigitalTwinUpdateRequestOptions requestOptions = default, CancellationToken cancellationToken = default)
        {
            return _protocolLayer.UpdateDigitalTwinWithHttpMessagesAsync(digitalTwinId, digitalTwinUpdateOperations, requestOptions?.IfMatch, null, cancellationToken);
        }

        /*/// <summary>
        /// Invoke a command on a digital twin asynchronously.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="commandName">The command to be invoked.</param>
        /// <param name="payload">The command payload.</param>
        /// <param name="connectTimeoutInSeconds">The time (in seconds) that the service waits for the device to come online. The default is 0 seconds (which means the device must already be online) and the maximum is 300 seconds.</param>
        /// <param name="responseTimeoutInSeconds">The time (in seconds) that the service waits for the method invocation to return a response. The default is 30 seconds, minimum is 5 seconds, and maximum is 300 seconds.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The application/json command invocation response and the http response. </returns>
        public async Task<HttpOperationResponse<string, DigitalTwinInvokeCommandHeaders>> InvokeCommandAsync(string digitalTwinId, string commandName, string payload, int? connectTimeoutInSeconds = default, int? responseTimeoutInSeconds = default, CancellationToken cancellationToken = default)
        {
            object commandPayload = JsonConvert.DeserializeObject<object>(payload);
            using HttpOperationResponse<object, DigitalTwinInvokeRootLevelCommandHeaders> response = await _protocolLayer.InvokeRootLevelCommandWithHttpMessagesAsync(digitalTwinId, commandName, commandPayload, connectTimeoutInSeconds, responseTimeoutInSeconds, null, cancellationToken)
                .ConfigureAwait(false);
            
            return new HttpOperationResponse<string, DigitalTwinInvokeCommandHeaders>
            {
                Body = JsonConvert.SerializeObject(response.Body),
                Headers = JsonConvert.DeserializeObject<DigitalTwinInvokeCommandHeaders>(JsonConvert.SerializeObject(response.Headers)),
                Request = response.Request,
                Response = response.Response
            };
        }

        /// <summary>
        /// Invoke a command on a digital twin asynchronously.
        /// </summary>
        /// <param name="digitalTwinId">The Id of the digital twin.</param>
        /// <param name="componentName">The component name under which the command is defined.</param>
        /// <param name="commandName">The command to be invoked.</param>
        /// <param name="payload">The command payload.</param>
        /// <param name="connectTimeoutInSeconds">The time (in seconds) that the service waits for the device to come online. The default is 0 seconds (which means the device must already be online) and the maximum is 300 seconds.</param>
        /// <param name="responseTimeoutInSeconds">The time (in seconds) that the service waits for the method invocation to return a response. The default is 30 seconds, minimum is 5 seconds, and maximum is 300 seconds.</param>
        /// <param name="cancellationToken">The cancellationToken.</param>
        /// <returns>The application/json command invocation response and the http response. </returns>
        public async Task<HttpOperationResponse<string, DigitalTwinInvokeCommandHeaders>> InvokeComponentCommandAsync(string digitalTwinId, string componentName, string commandName, string payload, int? connectTimeoutInSeconds = default, int? responseTimeoutInSeconds = default, CancellationToken cancellationToken = default)
        {
            object commandPayload = JsonConvert.DeserializeObject<object>(payload);
            using HttpOperationResponse<object, DigitalTwinInvokeComponentCommandHeaders> response = await _protocolLayer.InvokeComponentCommandWithHttpMessagesAsync(digitalTwinId, componentName, commandName, commandPayload, connectTimeoutInSeconds, responseTimeoutInSeconds, null, cancellationToken)
                .ConfigureAwait(false);

            return new HttpOperationResponse<string, DigitalTwinInvokeCommandHeaders>
            {
                Body = JsonConvert.SerializeObject(response.Body),
                Headers = JsonConvert.DeserializeObject<DigitalTwinInvokeCommandHeaders>(JsonConvert.SerializeObject(response.Headers)),
                Request = response.Request,
                Response = response.Response
            };
        }*/

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
