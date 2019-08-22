// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Exceptions;
using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.DigitalTwin.Client.Model.Callbacks;

namespace Microsoft.Azure.Devices.DigitalTwin.Client
{
    public abstract class DigitalTwinInterface
    {
        private DigitalTwinClient digitalTwinClient;

        public string Id { get; private set; }
        public string InstanceName { get; private set; }
        internal CommandCallback CommandHandler { get; private set; }
        internal PropertyUpdatedCallback PropertyUpdatedHandler { get; private set; }

        internal void Initialize(DigitalTwinClient dtClient)
        {
            digitalTwinClient = dtClient;
        }

        /// <summary>
        /// Creates an instance of <see cref="DigitalTwinInterface"/> 
        /// </summary>
        /// <param name="interfaceId">the interface id. </param>
        /// <param name="interfaceInstanceName">the interface instance name. </param>
        /// /// <param name="callbacks">the interface's callbacks. </param>
        public DigitalTwinInterface(string interfaceId, string interfaceInstanceName, Callbacks callbacks = null)
        {
            GuardHelper.ThrowIfInvalidInterfaceId(interfaceId, nameof(interfaceId));
            GuardHelper.ThrowIfInterfaceIdLengthInvalid(interfaceId, nameof(interfaceId));
            Id = interfaceId;
            InstanceName = interfaceInstanceName;
            if (callbacks != null)
            {
                CommandHandler = callbacks.CommandCB;
                PropertyUpdatedHandler = callbacks.PropertyUpdatedCB;
            }
        }

        #region report property
        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="property">The digital twin property.</param>
        protected async Task ReportPropertiesAsync(Memory<byte> properties)
        {
            await ReportPropertiesAsync(properties, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="property">The digital twin property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task ReportPropertiesAsync(Memory<byte> properties, CancellationToken cancellationToken)
        {
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.ReportPropertiesAsync(Id, InstanceName, properties, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region telemetry
        /// <summary>
        /// Sends an telemetry event to the cloud service.
        /// </summary>
        /// <parm name="telemetryValue">The telemetry name and telemetry value.</parm>
        protected async Task SendTelemetryAsync(string telemetryName, Memory<Byte> telemetryValue)
        {
            await SendTelemetryAsync(telemetryName, telemetryValue, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an telemetry event to cloud service.
        /// </summary>
        /// <parm name="telemetryValue">The telemetry name and telemetry value.</parm>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task SendTelemetryAsync(string telemetryName, Memory<Byte> telemetryValue, CancellationToken cancellationToken)
        {
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.SendTelemetryAsync(Id, InstanceName, telemetryName, telemetryValue, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region command
        /// <summary>
        /// Sends an update of the status of a pending asynchronous command. 
        /// </summary>
        /// <parm name="telemetryValue">The telemetry event, with telemetry name and telemetry value.</parm>
        protected async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update)
        {
            await UpdateAsyncCommandStatusAsync(update, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an update of the status of a pending asynchronous command. 
        /// </summary>
        /// <parm name="telemetryValue">The telemetry event, with telemetry name and telemetry value.</parm>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task UpdateAsyncCommandStatusAsync(DigitalTwinAsyncCommandUpdate update, CancellationToken cancellationToken)
        {
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.UpdateAsyncCommandStatusAsync(Id, InstanceName, update, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region helper
        private void ThrowIfInterfaceNotRegistered()
        {
            if (digitalTwinClient == null)
            {
                var errorMessage = string.Format(CultureInfo.InvariantCulture, DigitalTwinConstants.DeviceInterfaceNotRegisteredErrorMessageFormat, Id);
                throw new DigitalTwinDeviceInterfaceNotRegisteredException(errorMessage);
            }
        }
        #endregion
    }
}
