// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Generated;
using Microsoft.Azure.Devices.Generated.Models;
using Microsoft.Azure.Devices.Authentication;
using Microsoft.Rest;

namespace Microsoft.Azure.Devices
{
    public class PlugAndPlayClient : IDisposable
    {
        private readonly IotHubGatewayServiceAPIs _client;
        private readonly DigitalTwin _protocolLayer;

        /// <summary>
        /// Initializes a new instance of the <see cref="PlugAndPlayClient"/> class.</summary>
        /// <param name="connectionString">Your IoT hub's connection string.</param>
        public static PlugAndPlayClient CreateFromConnectionString(string connectionString)
        {
            connectionString.ThrowIfNullOrWhiteSpace(nameof(connectionString));

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            var sharedAccessKeyCredential = new SharedAccessKeyCredentials(connectionString);
            return new PlugAndPlayClient(iotHubConnectionString.HttpsEndpoint, sharedAccessKeyCredential);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PlugAndPlayClient"/> class.
        /// The client will use the provided endpoint and will generate credentials for each request using the provided <paramref name="credentials"/>.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="credentials">The SAS token provider to use for authorization.</param>
        public static PlugAndPlayClient CreateFromServiceCredentials(Uri endpoint, IotServiceClientCredentials credentials)
        {
            endpoint.ThrowIfNull(nameof(endpoint));
            credentials.ThrowIfNull(nameof(credentials));

            return new PlugAndPlayClient(endpoint, credentials);
        }

        /// <summary> Initializes a new instance of the <see cref="PlugAndPlayClient"/> class.</summary>
        protected PlugAndPlayClient()
        {
            // for mocking purposes only
        }

        private PlugAndPlayClient(Uri uri, IotServiceClientCredentials credentials)
        {
            _client = new IotHubGatewayServiceAPIs(uri, credentials);
            _protocolLayer = new DigitalTwin(_client);
        }

        public Task<HttpOperationResponse<object, DigitalTwinGetDigitalTwinHeaders>> GetDigitalTwin(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            return _protocolLayer.GetDigitalTwinWithHttpMessagesAsync(digitalTwinId, null, cancellationToken);
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
