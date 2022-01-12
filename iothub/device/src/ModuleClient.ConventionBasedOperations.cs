// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// Contains methods that a convention based module can use to send telemetry to the service,
    /// respond to commands and perform operations on its properties.
    /// </summary>
    /// <threadsafety static="true" instance="true" />
    public partial class ModuleClient : IDisposable
#if !NET451 && !NET472 && !NETSTANDARD2_0
        , IAsyncDisposable
#endif
    {
        /// <summary>
        /// The <see cref="PayloadConvention"/> that the client uses for convention-based operations.
        /// </summary>
        public PayloadConvention PayloadConvention => InternalClient.PayloadConvention;

        /// <summary>
        /// Send telemetry using the specified message.
        /// </summary>
        /// <remarks>
        /// Use the <see cref="TelemetryMessage(string)"/> constructor to pass in the optional component name
        /// that the telemetry message is from.
        /// </remarks>
        /// <param name="telemetryMessage">The telemetry message.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        public Task SendTelemetryAsync(TelemetryMessage telemetryMessage, CancellationToken cancellationToken = default)
            => InternalClient.SendTelemetryAsync(telemetryMessage, cancellationToken);

        /// <summary>
        /// Set the global command callback handler.
        /// </summary>
        /// <param name="callback">A method implementation that will handle the incoming command.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        public Task SubscribeToCommandsAsync(
            Func<CommandRequest, object, Task<CommandResponse>> callback, object userContext,
            CancellationToken cancellationToken = default)
            => InternalClient.SubscribeToCommandsAsync(callback, userContext, cancellationToken);

        /// <summary>
        /// Retrieve the client properties.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The device properties.</returns>
        public Task<ClientProperties> GetClientPropertiesAsync(CancellationToken cancellationToken = default)
            => InternalClient.GetClientTwinPropertiesAsync(cancellationToken);

        /// <summary>
        /// Update the client properties.
        /// This operation enables the partial update of the properties of the connected client.
        /// </summary>
        /// <param name="propertyCollection">Reported properties to push.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        /// <returns>The response of the update operation.</returns>
        public Task<ClientPropertiesUpdateResponse> UpdateClientPropertiesAsync(ClientPropertyCollection propertyCollection, CancellationToken cancellationToken = default)
            => InternalClient.UpdateClientPropertiesAsync(propertyCollection, cancellationToken);

        /// <summary>
        /// Sets the global listener for writable property update events.
        /// </summary>
        /// <param name="callback">The global call back to handle all writable property updates.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
        public Task SubscribeToWritablePropertyUpdateRequestsAsync(Func<ClientPropertyCollection, object, Task> callback, object userContext, CancellationToken cancellationToken = default)
            => InternalClient.SubscribeToWritablePropertyUpdateRequestsAsync(callback, userContext, cancellationToken);
    }
}
