// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

using Azure.Iot.DigitalTwin.Device.Exceptions;
using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;

using static Azure.Iot.DigitalTwin.Device.Model.Callbacks;

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
        /// <param name="id">the interface id. </param>
        /// <param name="instanceName">the interface instance name. </param>
        /// /// <param name="callbacks">the interface's callbacks. </param>
        public DigitalTwinInterfaceClient(string id, string instanceName, Callbacks callbacks)
        {
            GuardHelper.ThrowIfInvalidInterfaceId(id, nameof(id));
            GuardHelper.ThrowIfInterfaceIdLengthInvalid(id, nameof(id));
            this.Id = id;
            this.InstanceName = instanceName;
            if (callbacks != null)
            {
                this.CommandHandler = callbacks.CommandCB;
                this.PropertyUpdatedHandler = callbacks.PropertyUpdatedCB;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinInterfaceClient"/> class.
        /// </summary>
        /// <param name="id">the interface id. </param>
        /// <param name="instanceName">the interface instance name. </param>
        public DigitalTwinInterfaceClient(string id, string instanceName)
            : this(id, instanceName, null)
        {
        }

        /// <summary>
        /// Gets the interface instance name associated with this interface.
        /// </summary>
        public string InstanceName { get; private set; }

        /// <summary>
        /// Gets the interface id associated with this interface.
        /// </summary>
        public string Id { get; private set; }

        /// <summary>
        /// Gets the command handler associated with  this interface.
        /// </summary>
        internal CommandCallback CommandHandler { get; private set; }

        /// <summary>
        /// Gets the property updated handler associated with  this interface.
        /// </summary>
        internal PropertyUpdatedCallback PropertyUpdatedHandler { get; private set; }

        internal void Initialize(DigitalTwinClient dtClient)
        {
            this.digitalTwinClient = dtClient;
        }

        /// <summary>
        /// Reports properties to the cloud service.
        /// </summary>
        /// <param name="properties">The serialized json representing the property key and value pair(s) to be reported.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task ReportPropertiesAsync(Memory<byte> properties)
        {
            await this.ReportPropertiesAsync(properties, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="properties">The serialized json containing the property key and value pair(s) to be reported.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task ReportPropertiesAsync(Memory<byte> properties, CancellationToken cancellationToken)
        {
            this.ThrowIfInterfaceNotRegistered();
            await this.digitalTwinClient.ReportPropertiesAsync(this.InstanceName, properties, cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an telemetry event to the cloud service.
        /// </summary>
        /// <param name="telemetryName">>The telemetry name to be sent.</param>
        /// <param name="telemetryValue">The serialized representation the telemetry value to be sent.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        protected async Task SendTelemetryAsync(string telemetryName, Memory<byte> telemetryValue)
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
        protected async Task SendTelemetryAsync(string telemetryName, Memory<byte> telemetryValue, CancellationToken cancellationToken)
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
