// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.DigitalTwin.Client.Exceptions;
using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.DigitalTwin.Client
{
    public abstract class DigitalTwinInterface
    {
        private DigitalTwinClient digitalTwinClient;

        public string Id { get; private set; }
        public string InstanceName { get; private set; }

        public delegate Task DigitalTwinPropertyCallback(DigitalTwinValue propertyUpdate, long desiredVersion, object userContext);
        public delegate Task<DigitalTwinCommandResponse> CommandCallback(DigitalTwinCommandRequest commandRequest, object userContext);

        internal void Initialize(DigitalTwinClient dtClient)
        {
            digitalTwinClient = dtClient;
        }

        /// <summary>
        /// Creates an instance of <see cref="DigitalTwinInterface"/> 
        /// </summary>
        /// <param name="interfaceId">the interface id. </param>
        /// <param name="interfaceId">the interface instance name. </param>
        public DigitalTwinInterface(string interfaceId, string interfaceInstanceName)
        {
            GuardHelper.ThrowIfInvalidInterfaceId(interfaceId, nameof(interfaceId));
            GuardHelper.ThrowIfInterfaceIdLengthInvalid(interfaceId, nameof(interfaceId));
            Id = interfaceId;
            InstanceName = interfaceInstanceName;
        }

        #region report property
        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="property">The digital twin property.</param>
        protected async Task ReportPropertiesAsync(IEnumerable<DigitalTwinProperty> properties)
        {
            await ReportPropertiesAsync(properties, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Reports property to the cloud service.
        /// </summary>
        /// <param name="property">The digital twin property.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task ReportPropertiesAsync(IEnumerable<DigitalTwinProperty> properties, CancellationToken cancellationToken)
        {
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.ReportPropertiesAsync(Id, InstanceName, properties, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region Set property update callback
        /// <summary>
        /// Registers a delegate to receive property value update from the cloud service. If a delegate is already registered 
        /// for the given property, it will replaced with the new delegate.
        /// </summary>
        /// <param name="propertyName">The name of the read-write property to associate with the callback. Cannot be null reference, empty string or white space.</param>
        /// <param name="propertyHandler">The callback to invoke when read-write property has been updated.</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        protected async Task SetPropertyUpdatedCallbackAsync(string propertyName, DigitalTwinPropertyCallback propertyHandler, object userContext)
        {
            await SetPropertyUpdatedCallbackAsync(propertyName, propertyHandler, userContext, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a delegate to receive property value update from the cloud service. If a delegate is already registered 
        /// for the given read-write property, it will replaced with the new delegate.
        /// </summary>
        /// <param name="propertyName">The name of the read-write property to associate with the callback. Cannot be null reference, empty string or white space.</param>
        /// <param name="propertyHandler">The callback to invoke when read-write property has been updated.</param>
        /// <param name="userContext">Context object that will be passed into callback</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task SetPropertyUpdatedCallbackAsync(string propertyName, DigitalTwinPropertyCallback propertyHandler, object userContext, CancellationToken cancellationToken)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(propertyName, nameof(propertyName));
            GuardHelper.ThrowIfNull(propertyHandler, nameof(propertyHandler));
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.SetPropertyUpdatedCallbackAsync(Id, InstanceName, propertyName, propertyHandler, userContext, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region telemetry
        /// <summary>
        /// Sends an telemetry event to the cloud service.
        /// </summary>
        /// <parm name="telemetryValue">The telemetry name and telemetry value.</parm>
        protected async Task SendTelemetryAsync(DigitalTwinProperty telemetryData)
        {
            await SendTelemetryAsync(telemetryData, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Sends an telemetry event to cloud service.
        /// </summary>
        /// <parm name="telemetryValue">The telemetry name and telemetry value.</parm>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task SendTelemetryAsync(DigitalTwinProperty telemetryData, CancellationToken cancellationToken)
        {
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.SendTelemetryAsync(Id, InstanceName, telemetryData, cancellationToken).ConfigureAwait(false);
        }
        #endregion

        #region command
        /// <summary>
        /// Registers a delegate for a given command. If a delegate is already registered 
        /// for the given command, it will replaced with the new delegate.
        /// </summary>
        /// <param name="commandName">The name of command to associate with the callback.</param>
        /// <param name="commandCallback">The callback to invoke when command is called by the cloud service.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        protected async Task SetCommandCallbackAsync(string commandName, CommandCallback commandCallback, object userContext)
        {
            await SetCommandCallbackAsync(commandName, commandCallback, userContext, CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Registers a delegate for a given command. If a delegate is already registered 
        /// for the given command, it will replaced with the new delegate.
        /// </summary>
        /// <param name="commandName">The name of command to associate with the callback.</param>
        /// <param name="commandCallback">The callback to invoke when command is called by the cloud service.</param>
        /// <param name="userContext">Generic parameter to be interpreted by the client code.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        protected async Task SetCommandCallbackAsync(string commandName, CommandCallback commandCallback, object userContext, CancellationToken cancellationToken)
        {
            GuardHelper.ThrowIfNullOrWhiteSpace(commandName, nameof(commandName));
            GuardHelper.ThrowIfNull(commandCallback, nameof(commandCallback));
            ThrowIfInterfaceNotRegistered();
            await digitalTwinClient.SetCommandCallbackAsync(Id, InstanceName, commandName, commandCallback, userContext, cancellationToken).ConfigureAwait(false);
        }

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
