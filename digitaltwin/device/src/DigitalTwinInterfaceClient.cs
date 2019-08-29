// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device.Exceptions;
using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;

namespace Azure.Iot.DigitalTwin.Device
{
    /// <summary>
    /// Digital Twin Interface Client implementations to receive requests on this interface from
    /// the server (namely commands and property updates) and to send data from the interface to
    /// the server (namely reported properties and telemetry).
    /// </summary>
    public abstract class DigitalTwinInterfaceClient
    {
        private DigitalTwinClient digitalTwinClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinInterfaceClient"/> class.
        /// </summary>
        /// <param name="id">the interface id.</param>
        /// <param name="instanceName">the interface instance name.</param>
        /// <param name="isCommandEnabled">indicates if command is implemented in the interface.</param>
        /// <param name="isPropertyUpdatedEnabled">indicates if property updated is required in the interface.</param>
        protected DigitalTwinInterfaceClient(string id, string instanceName, bool isCommandEnabled, bool isPropertyUpdatedEnabled)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(id, nameof(id));
            GuardHelper.ThrowIfInvalidInterfaceId(id, nameof(id));
            GuardHelper.ThrowIfInterfaceIdLengthInvalid(id, nameof(id));

            GuardHelper.ThrowIfNullOrWhiteSpace(instanceName, nameof(instanceName));

            this.Id = id;
            this.InstanceName = instanceName;
            this.IsCommandEnabled = isCommandEnabled;
            this.IsPropertyUpdatedEnabled = isPropertyUpdatedEnabled;
        }

        /// <summary>
        /// Gets the interface instance name associated with this interface.
        /// </summary>
        internal string InstanceName { get; private set; }

        /// <summary>
        /// Gets the interface id associated with this interface.
        /// </summary>
        internal string Id { get; private set; }

        internal bool IsCommandEnabled { get; private set; }

        internal bool IsPropertyUpdatedEnabled { get; private set; }

        /// <summary>
        /// Callback for commands.  Triggers when a command is received at an interface.
        /// </summary>
        /// <param name="commandRequest">incoming command request.</param>
        /// <returns>DigitalTwinCommandResponse.</returns>
        public virtual Task<DigitalTwinCommandResponse> OnCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            return Task.FromResult(new DigitalTwinCommandResponse(404));
        }

        /// <summary>
        /// Callback for property updates.  Triggers when a property update is received at an interface.
        /// </summary>
        /// <param name="propertyUpdate">incoming property updated notification.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        public virtual Task OnPropertyUpdated(DigitalTwinPropertyUpdate propertyUpdate)
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Initialize the digital twin.
        /// </summary>
        /// <param name="digitalTwinClient">the DigitalTwinClient associated with this interface instance.</param>
        internal void Initialize(DigitalTwinClient digitalTwinClient)
        {
            this.digitalTwinClient = digitalTwinClient;
        }

        /// <summary>
        /// Reports properties to the cloud service.
        /// </summary>
        /// <param name="properties">The serialized json representing the property key and value pair(s) to be reported.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task ReportPropertiesAsync(IEnumerable<DigitalTwinPropertyReport> properties)
        {
            await this.ReportPropertiesAsync(properties, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="properties">The serialized json containing the property key and value pair(s) to be reported.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task ReportPropertiesAsync(IEnumerable<DigitalTwinPropertyReport> properties, CancellationToken cancellationToken)
        {
            GuardHelper.ThrowIfNull(properties, nameof(properties));
            this.ThrowIfInterfaceNotRegistered();
            await this.digitalTwinClient.ReportPropertiesAsync(this.InstanceName, properties, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an telemetry event to the cloud service.
        /// </summary>
        /// <param name="telemetryName">>The telemetry name to be sent.</param>
        /// <param name="telemetryValue">The serialized representation the telemetry value to be sent.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task SendTelemetryAsync(string telemetryName, string telemetryValue)
        {
            await this.SendTelemetryAsync(telemetryName, telemetryValue, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an telemetry event to cloud service.
        /// </summary>
        /// <param name="telemetryName">The telemetry name to be sent.</param>
        /// <param name="telemetryValue">The serialized representation the telemetry value to be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task SendTelemetryAsync(string telemetryName, string telemetryValue, CancellationToken cancellationToken)
        {
            this.ThrowIfInterfaceNotRegistered();
            await this.digitalTwinClient.SendTelemetryAsync(this.Id, this.InstanceName, telemetryName, telemetryValue, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an update of the status of a pending asynchronous command.
        /// </summary>
        /// <param name="update">The serialized json representing the property key and value pair(s) to be reported.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update)
        {
            await this.UpdateAsyncCommandStatusAsync(update, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an update of the status of a pending asynchronous command.
        /// </summary>
        /// <param name="update">The serialized json representing the property key and value pair(s) to be reported.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update, CancellationToken cancellationToken)
        {
            this.ThrowIfInterfaceNotRegistered();
            await this.digitalTwinClient.UpdateAsyncCommandStatusAsync(this.Id, this.InstanceName, update, cancellationToken).ConfigureAwait(false);
        }

        private void ThrowIfInterfaceNotRegistered()
        {
            if (this.digitalTwinClient == null)
            {
                var errorMessage = string.Format(CultureInfo.InvariantCulture, DigitalTwinConstants.DeviceInterfaceNotRegisteredErrorMessageFormat, this.InstanceName);
                throw new DigitalTwinDeviceInterfaceNotRegisteredException(errorMessage);
            }
        }
    }
}
