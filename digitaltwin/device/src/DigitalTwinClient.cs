// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.DigitalTwin.Client.Bindings;
using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Microsoft.Azure.Devices.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.DigitalTwin.Client.DigitalTwinInterface;

namespace Microsoft.Azure.Devices.DigitalTwin.Client
{
    public class DigitalTwinClient
    {
        private const string CapabilityModelIdTag = "capabilityModelId";
        private const string InterfacesTag = "interfaces";
        private const string InterfacesPrefix = "$iotin:";

        private const string ModelDiscoveryInterfaceId = "urn:azureiot:ModelDiscovery:ModelInformation:1";
        private const string ModelDiscoveryInterfaceInstanceName = "urn:azureiot:ModelDiscovery:ModelInformation";
        private const string ModelInformationSchema = "modelInformation";

        private readonly DeviceClient deviceClient;
        private Dictionary<string, Tuple<DigitalTwinPropertyCallback, object>> propertyCallbacksWithUserContext;
        private Dictionary<string, CommandCallback> commandCallbacks;
        private readonly DigitalTwinBindingFormatterCollection digitalTwinFormatterCollection;

        public DigitalTwinClient(DeviceClient deviceClient)
        {
            this.deviceClient = deviceClient;
            this.propertyCallbacksWithUserContext = new Dictionary<string, Tuple<DigitalTwinPropertyCallback, object>>();
            this.commandCallbacks = new Dictionary<string, CommandCallback>();
            this.digitalTwinFormatterCollection = new DigitalTwinBindingFormatterCollection();
        }

        #region Register Interfaces
        /// <summary>
        /// Register list of interfaces. This method will replace any previously registered interfaces.
        /// </summary>
        /// <param name="dtInterfaces">The list of digital twin interfaces.</param>
        /// <parm name="cancellationToken">The cancellation token.</parm>
        public async Task RegisterInterfacesAsync(string capabilityModelId, DigitalTwinInterface[] dtInterfaces, CancellationToken cancellationToken)
        {
            var interfaceName = new Dictionary<string, string>();

            foreach (var dtInterface in dtInterfaces)
            {
                GuardHelper.ThrowIfNull(dtInterface, nameof(dtInterface));
                interfaceName.Add(dtInterface.InstanceName, dtInterface.Id);
            }

            var capabilityModelIdProperty = new DigitalTwinProperty(CapabilityModelIdTag, DigitalTwinValue.CreateString(capabilityModelId));

            var interfacesProperty = new DigitalTwinProperty(
                InterfacesTag,
                DigitalTwinValue.CreateObject(new DataCollection(digitalTwinFormatterCollection.FromObject(interfaceName))));

            // send register interface 
            await deviceClient.SendEventAsync(
                CreateRegistrationMessage(new[] { capabilityModelIdProperty, interfacesProperty }), cancellationToken).ConfigureAwait(false);

            foreach (var dtInterface in dtInterfaces)
            {
                dtInterface.Initialize(this);
            }

            // TODO: send device information
        }

        /// <summary>
        /// Register list of interfaces. This method will replace any previously registered interfaces.
        /// </summary>
        /// <param name="digitalTwinInterfaces">The list of digital twin interfaces.</param>
        public async Task RegisterInterfacesAsync(string capabilityModelId, DigitalTwinInterface[] digitalTwinInterfaces)
        {
            await RegisterInterfacesAsync(capabilityModelId, digitalTwinInterfaces, CancellationToken.None).ConfigureAwait(false);
        }
        #endregion

        #region IoTHubOperations 
        internal async Task ReportPropertiesAsync(string interfaceId, string instanceName, IEnumerable<DigitalTwinProperty> properties, CancellationToken cancellationToken)
        {
            TwinCollection twinCollection = new TwinCollection();
            foreach (DigitalTwinProperty property in properties)
            {
                twinCollection[property.Name] = new Dictionary<string, object> { { "value", property.RawValue } };
            }

            cancellationToken.ThrowIfCancellationRequested();
            await deviceClient.UpdateReportedPropertiesAsync(
                CreateKeyValueTwinCollection(InterfacesPrefix + instanceName, twinCollection)).ConfigureAwait(false);
        }

        internal async Task SetPropertyUpdatedCallbackAsync(string interfaceId, string instanceName, string propertyName, DigitalTwinPropertyCallback propertyHandler, object userContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal async Task SendTelemetryAsync(string interfaceId, string interfaceInstanceName, DigitalTwinProperty telemetryValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await deviceClient.SendEventAsync(CreateDigitalTwinMessage(interfaceId, interfaceInstanceName, telemetryValue), cancellationToken).ConfigureAwait(false);
        }
        internal async Task SetCommandCallbackAsync(string interfaceId, string instanceName, string commandName, CommandCallback commandHandler, object userContext, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        internal async Task UpdateAsyncCommandStatusAsync(string interfaceId, string instanceName, DigitalTwinAsyncCommandUpdate update, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private static TwinCollection CreateKeyValueTwinCollection(string key, object value)
        {
            TwinCollection json = new TwinCollection();
            json[key] = value;
            return json;
        }

        private static DataCollection CreateKeyValueDataCollection(string key, object value)
        {
            DataCollection json = new DataCollection();
            json[key] = value;
            return json;
        }

        private static DataCollection CreateKeyValueDataCollection(IEnumerable<KeyValuePair<string, object>> keyValuePairs)
        {
            DataCollection json = new DataCollection();

            foreach (var keyValuePair in keyValuePairs)
            {
                json[keyValuePair.Key] = keyValuePair.Value;
            }

            return json;
        }

        private Message CreateRegistrationMessage(IEnumerable<DigitalTwinProperty> telemetryValues)
        {       
            Message message = new Message(Encoding.UTF8.GetBytes(
                digitalTwinFormatterCollection.FromObject(
                        CreateKeyValueDataCollection(
                            telemetryValues.Select(v => new KeyValuePair<string, object>(v.Name, v.RawValue))))));
            message.Properties.Add(DigitalTwinConstants.IoTHubInterfaceId, ModelDiscoveryInterfaceId);
            message.Properties.Add(DigitalTwinConstants.IothubInterfaceInstance, ModelDiscoveryInterfaceInstanceName);
            message.ContentType = DigitalTwinConstants.JsonContentType;
            message.MessageSchema = ModelInformationSchema;
            return message;
        }

        private Message CreateDigitalTwinMessage(string interfaceId, string interfaceInstanceId, DigitalTwinProperty telemetryValue)
        {
            Message message = new Message(
                Encoding.UTF8.GetBytes(
                    digitalTwinFormatterCollection.FromObject(
                        CreateKeyValueDataCollection(telemetryValue.Name, telemetryValue.RawValue))));
            message.Properties.Add(DigitalTwinConstants.IothubInterfaceInstance, interfaceInstanceId);
            message.Properties.Add(DigitalTwinConstants.IoTHubInterfaceId, interfaceId);
            message.ContentType = DigitalTwinConstants.JsonContentType;
            message.MessageSchema = telemetryValue.Name;
            return message;
        }
        #endregion
    }
}
