// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Microsoft.Azure.Devices.DigitalTwin.Service.Generated;
using Microsoft.Azure.Devices.DigitalTwin.Service.Models;

namespace Microsoft.Azure.Devices.DigitalTwin.Service
{
    /// <summary>
    /// Service client for getting digital twin interfaces, invoking interface commands, updating digital twin state, and retrieving model definitions.
    /// </summary>
    public partial class DigitalTwinServiceClient
    {
        private Generated.DigitalTwin digitalTwin;
        private const string _apiVersion = "2019-07-01-preview";

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinServiceClient"/> class.</summary>
        /// <param name="connectionString">Your IoT hub's connection string.</param>
        public DigitalTwinServiceClient(string connectionString)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

            var iothubConnectionStringParser = ServiceConnectionStringParser.Create(connectionString);
            ServiceConnectionString iothubServiceConnectionString = new ServiceConnectionString(iothubConnectionStringParser);
            IoTServiceClientCredentials serviceClientCredentials = new SharedAccessKeyCredentials(iothubServiceConnectionString);
            this.SetupDigitalTwinServiceClient(iothubServiceConnectionString.HttpsEndpoint, serviceClientCredentials);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinServiceClient"/> class.
        /// The client will use the provided endpoint and will generate credentials for each request using the provided <paramref name="credentials"/>.
        /// </summary>
        /// <param name="endpoint">The endpoint to connect to.</param>
        /// <param name="credentials">The SAS token provider to use for authorization.</param>
        /// <param name="options">The options for the client instance to use.</param>
        public DigitalTwinServiceClient(Uri endpoint, IoTServiceClientCredentials credentials)
        {
            GuardHelper.ThrowIfNull(endpoint, nameof(endpoint));
            GuardHelper.ThrowIfNull(credentials, nameof(credentials));

            this.SetupDigitalTwinServiceClient(endpoint, credentials);
        }

        /// <summary> Initializes a new instance of the <see cref="DigitalTwinServiceClient"/> class.</summary>
        protected DigitalTwinServiceClient()
        {
            // for mocking purposes only
        }

        /// <summary>
        /// Retrieve the state of a single digital twin
        /// </summary>
        /// <param name="digitalTwinId">The id of the digital twin to get the state of.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>The state of the full digital twin, including all properties of all interface instances registered by that digital twin.</returns>
        public virtual Response<string> GetDigitalTwin(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return this.GetDigitalTwinAsync(digitalTwinId, cancellationToken).Result;
        }

        /// <summary>
        /// Retrieve the state of a single digital twin
        /// </summary>
        /// <param name="digitalTwinId">The id of the digital twin to get the state of.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>The state of the full digital twin, including all properties of all interface instances registered by that digital twin.</returns>
        public virtual async Task<Response<string>> GetDigitalTwinAsync(string digitalTwinId, CancellationToken cancellationToken = default)
        {
            string digitalTwinInterfaces = await this.digitalTwin.GetInterfacesAsync(digitalTwinId, cancellationToken).ConfigureAwait(false);

            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return new Response<string>(null, digitalTwinInterfaces);
        }

        /// <summary>
        /// Update one to many properties on one to many interface instances on one digital twin instance
        /// </summary>
        /// <param name="digitalTwinId">The digital twin to update.</param>
        /// <param name="interfaceInstanceName">The interface instance whose properties will be updated</param>
        /// <param name="patch">The JSON representation of the patch. For example, to update two separate properties on the interface instance "sampleDeviceInfo", the JSON should look like:
        /// {
        ///   "properties": {
        ///     "somePropertyName": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     },
        ///     "somePropertyName2": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     }
        ///   }
        /// }
        ///
        /// Nested properties are allowed, but the maximum depth allowed is 7.
        /// </param>
        /// <param name="etag">(Optional) The ETag of the digital twin.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>Returns the full updated digital twin representation.</returns>
        public virtual Response<string> UpdateDigitalTwinProperties(string digitalTwinId, string interfaceInstanceName, string patch, string etag, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return this.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, etag, cancellationToken).Result;
        }

        /// <summary>
        /// Update one to many properties on one to many interface instances on one digital twin instance
        /// </summary>
        /// <param name="digitalTwinId">The digital twin to update.</param>
        /// <param name="interfaceInstanceName">The interface instance whose properties will be updated</param>
        /// <param name="patch">The JSON representation of the patch. For example, to update two separate properties on the interface instance "sampleDeviceInfo", the JSON should look like:
        /// {
        ///   "properties": {
        ///     "somePropertyName": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     },
        ///     "somePropertyName2": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     }
        ///   }
        /// }
        ///
        /// Nested properties are allowed, but the maximum depth allowed is 7.
        /// </param>
        /// <param name="etag">(Optional) The ETag of the digital twin.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>Returns the full updated digital twin representation.</returns>
        public virtual async Task<Response<string>> UpdateDigitalTwinPropertiesAsync(string digitalTwinId, string interfaceInstanceName, string patch, string etag, CancellationToken cancellationToken = default)
        {
            string fullPatch = 
                "{" +
                "  \"interfaces\": {" +
                "    \"" + interfaceInstanceName + "\": " + patch +
                "  }" +
                "}";

            string digitalTwinInterfaces = await this.digitalTwin.UpdateInterfacesAsync(digitalTwinId, fullPatch, etag, cancellationToken).ConfigureAwait(false);

            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return new Response<string>(null, digitalTwinInterfaces);
        }

        /// <summary>
        /// Update one to many properties on one to many interface instances on one digital twin instance
        /// </summary>
        /// <param name="digitalTwinId">The digital twin to update.</param>
        /// <param name="interfaceInstanceName">The interface instance whose properties will be updated</param>
        /// <param name="patch">The JSON representation of the patch. For example, to update two separate properties on the interface instance "sampleDeviceInfo", the JSON should look like:
        /// {
        ///   "properties": {
        ///     "somePropertyName": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     },
        ///     "somePropertyName2": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     }
        ///   }
        /// }
        ///
        /// Nested properties are allowed, but the maximum depth allowed is 7.
        /// </param>
        /// <param name="etag">(Optional) The ETag of the digital twin.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>Returns the full updated digital twin representation.</returns>
        public virtual Response<string> UpdateDigitalTwinProperties(string digitalTwinId, string interfaceInstanceName, string patch, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return this.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, cancellationToken).Result;
        }

        /// <summary>
        /// Update one to many properties on one to many interface instances on one digital twin instance
        /// </summary>
        /// <param name="digitalTwinId">The digital twin to update.</param>
        /// <param name="interfaceInstanceName">The interface instance whose properties will be updated</param>
        /// <param name="patch">The JSON representation of the patch. For example, to update two separate properties on the interface instance "sampleDeviceInfo", the JSON should look like:
        /// {
        ///   "properties": {
        ///     "somePropertyName": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     },
        ///     "somePropertyName2": {
        ///       "desired": {
        ///         "value": "somePropertyValue"
        ///       }
        ///     }
        ///   }
        /// }

        ///
        /// Nested properties are allowed, but the maximum depth allowed is 7.
        /// </param>
        /// <param name="etag">(Optional) The ETag of the digital twin.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>Returns the full updated digital twin representation.</returns>
        public virtual async Task<Response<string>> UpdateDigitalTwinPropertiesAsync(string digitalTwinId, string interfaceInstanceName, string patch, CancellationToken cancellationToken = default)
        {
            return await this.UpdateDigitalTwinPropertiesAsync(digitalTwinId, interfaceInstanceName, patch, null, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Invoke a digital twin command on the given interface instance that is implemented by the given digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The digital twin to invoke the command on.</param>
        /// <param name="interfaceInstanceName">The name of the interface instance in that digital twin that the method belongs to.</param>
        /// <param name="commandName">The name of the command to be invoked.</param>
        /// <param name="argument">Additional information to be given to the device receiving the command. Must be UTF-8 encoded JSON string. May be null if no argument should be sent</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>The result of the command invocation. Like the argument given, it must be UTF-8 encoded JSON string.</returns>
        public virtual Response<DigitalTwinCommandResponse> InvokeCommand(string digitalTwinId, string interfaceInstanceName, string commandName, string argument, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return this.InvokeCommandAsync(digitalTwinId, interfaceInstanceName, commandName, argument, cancellationToken).Result;
        }

        /// <summary>
        /// Invoke a digital twin command on the given interface instance that is implemented by the given digital twin.
        /// </summary>
        /// <param name="digitalTwinId">The digital twin to invoke the command on.</param>
        /// <param name="interfaceInstanceName">The name of the interface instance in that digital twin that the method belongs to.</param>
        /// <param name="commandName">The name of the command to be invoked.</param>
        /// <param name="argument">Additional information to be given to the device receiving the command. Must be UTF-8 encoded JSON string. May be null if no argument should be sent</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>The result of the command invocation. Like the argument given, it must be UTF-8 encoded JSON string.</returns>
        public virtual async Task<Response<DigitalTwinCommandResponse>> InvokeCommandAsync(string digitalTwinId, string interfaceInstanceName, string commandName, string argument, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            var result = await this.digitalTwin.InvokeInterfaceCommandWithHttpMessagesAsync(digitalTwinId, interfaceInstanceName, commandName, argument, null, null, null, cancellationToken).ConfigureAwait(false);
            var commandResponse = new DigitalTwinCommandResponse(result.Headers.XMsRequestId, result.Headers.XMsCommandStatuscode, (string) result.Body);
            return new Response<DigitalTwinCommandResponse>(null, commandResponse);
        }

        /// <summary>
        /// Retrieve the Model representation for the given modelId.
        /// </summary>
        /// <param name="modelId">The id of the model to retrieve. For example: "urn:azureiot:DeviceManagement:DeviceInformation:1".</param>
        /// <param name="expand">Indicates whether to expand the device capability model's interface definitions in line or not.
        /// This query parameter ONLY applies to Capability model.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>The model representation that was retrieved.</returns>
        public virtual Response<string> GetModel(string modelId, bool expand = false, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return this.GetModelAsync(modelId, expand, cancellationToken).Result;
        }

        /// <summary>
        /// Retrieve the Model representation for the given modelId.
        /// </summary>
        /// <param name="modelId">The id of the model to retrieve. For example: "urn:azureiot:DeviceManagement:DeviceInformation:1".</param>
        /// <param name="expand">Indicates whether to expand the device capability model's interface definitions in line or not.
        /// This query parameter ONLY applies to Capability model.</param>
        /// <param name="cancellationToken">(Optional) The cancellation token.</param>
        /// <returns>The model representation that was retrieved.</returns>
        public virtual async Task<Response<string>> GetModelAsync(string modelId, bool expand = false, CancellationToken cancellationToken = default)
        {
            // TODO issue #6 since auto-generated code lacks pipeline support, the HTTP response object portion of the Response<T> will always be null
            return new Response<string>(null, (string)await this.digitalTwin.GetDigitalTwinModelAsync(modelId, expand, cancellationToken).ConfigureAwait(false));
        }

        private void SetupDigitalTwinServiceClient(Uri uri, IoTServiceClientCredentials credentials)
        {
            DelegatingHandler[] handlers = new DelegatingHandler[1] { new AuthorizationDelegatingHandler(credentials) };
            var protocolLayer = new IotHubGatewayServiceAPIs20190701Preview(credentials, handlers)
            {
                ApiVersion = _apiVersion,
                BaseUri = uri,
            };
            this.digitalTwin = new Generated.DigitalTwin(protocolLayer);
        }
    }
}