// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device.Exceptions;
using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;
using Azure.IoT.DigitalTwin.Device;

namespace Azure.Iot.DigitalTwin.Device
{
    /// <summary>
    /// Digital Twin Interface Client implementations to receive requests on this interface from
    /// the server (namely commands and property updates) and to send data from the interface to
    /// the server (namely reported properties and telemetry).
    /// </summary>
    public abstract class DigitalTwinInterfaceClient
    {
        /// <summary>
        /// Status code for completed.
        /// </summary>
        public const int StatusCodeCompleted = 200;

        /// <summary>
        /// Status code for pending operation.
        /// </summary>
        public const int StatusCodePending = 202;

        /// <summary>
        /// Status code for invalid operation.
        /// </summary>
        public const int StatusCodeInvalid = 400;

        /// <summary>
        /// Status code for not implemented.
        /// </summary>
        public const int StatusCodeNotImplemented = 404;

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
            GuardHelper.ThrowIfNullOrWhiteSpace(instanceName, nameof(instanceName));
            GuardHelper.ThrowIfInvalidInterfaceInstanceName(instanceName, nameof(instanceName));

            this.Id = id;
            this.InstanceName = instanceName;
            this.IsCommandEnabled = isCommandEnabled;
            this.IsPropertyUpdatedEnabled = isPropertyUpdatedEnabled;
        }

        /// <summary>
        /// Gets the interface instance name associated with this interface.
        /// </summary>
        internal string InstanceName { get; }

        /// <summary>
        /// Gets the interface id associated with this interface.
        /// </summary>
        internal string Id { get; }

        internal bool IsCommandEnabled { get; }

        internal bool IsPropertyUpdatedEnabled { get; }

        /// <summary>
        /// Initialize the digital twin.
        /// </summary>
        /// <param name="digitalTwinClient">the DigitalTwinClient associated with this interface instance.</param>
        internal void Initialize(DigitalTwinClient digitalTwinClient)
        {
            this.digitalTwinClient = digitalTwinClient;
            this.OnRegistrationCompleted();
        }

        /// <summary>
        /// Callback for commands. Triggers when a command is received at an interface.
        /// Interfaces implementation is expected to override it.
        /// </summary>
        /// <param name="commandRequest">incoming command request.</param>
        /// <returns>DigitalTwinCommandResponse.</returns>
        protected internal virtual Task<DigitalTwinCommandResponse> OnCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            Logging.Instance.LogInformational("Command not being handled at interface.");
            return Task.FromResult(new DigitalTwinCommandResponse(StatusCodeNotImplemented));
        }

        /// <summary>
        /// Callback for property updates.  Triggers when a property update is received at an interface.
        /// Interfaces implementation is expected to override it.
        /// </summary>
        /// <param name="propertyUpdate">incoming property updated notification.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        protected internal virtual Task OnPropertyUpdated(DigitalTwinPropertyUpdate propertyUpdate)
        {
            Logging.Instance.LogInformational("Property updated not being handled at interface.");
            return Task.CompletedTask;
        }

        /// <summary>
        /// Should contains interface initialization process.  Triggers when register interface is completed and signal
        /// interface to proceed with initialization. Interfaces should implement it.
        /// </summary>
        /// <param name="propertyUpdate">incoming property updated notification.</param>
        protected internal virtual void OnRegistrationCompleted()
        {
            Logging.Instance.LogInformational("DigitalTwinInterfaceClient registered.");
        }

        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="properties">The serialized json containing the property key and value pair(s) to be reported.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task ReportPropertiesAsync(IEnumerable<DigitalTwinPropertyReport> properties, CancellationToken cancellationToken = default)
        {
            GuardHelper.ThrowIfNull(properties, nameof(properties));
            this.ThrowIfInterfaceNotRegistered();
            await this.digitalTwinClient.ReportPropertiesAsync(this.InstanceName, properties, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an telemetry event to cloud service.
        /// </summary>
        /// <param name="telemetryName">The telemetry name to be sent.</param>
        /// <param name="telemetryValue">The serialized representation the telemetry value to be sent.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task SendTelemetryAsync(string telemetryName, string telemetryValue, CancellationToken cancellationToken = default)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(telemetryName, nameof(telemetryName));
            GuardHelper.ThrowIfNullOrWhiteSpace(telemetryValue, nameof(telemetryValue));
            this.ThrowIfInterfaceNotRegistered();
            await this.digitalTwinClient.SendTelemetryAsync(this.Id, this.InstanceName, telemetryName, telemetryValue, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an update of the status of a pending asynchronous command.
        /// </summary>
        /// <param name="update">The serialized json representing the property key and value pair(s) to be reported.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update, CancellationToken cancellationToken = default)
        {
            update.Validate();
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
