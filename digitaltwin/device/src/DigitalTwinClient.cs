// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.DigitalTwin.Client.Bindings;
using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;
using Microsoft.Azure.Devices.DigitalTwin.Client.Model;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static Microsoft.Azure.Devices.DigitalTwin.Client.Model.Callbacks;

namespace Microsoft.Azure.Devices.DigitalTwin.Client
{
    public class DigitalTwinClient
    {
        private const string CapabilityModelIdTag = "capabilityModelId";
        private const string InterfacesTag = "interfaces";
        private const string InterfacesPrefix = "$iotin:";
        private const string IothubInterfaceInstance = "$.ifname";
        private const string IoTHubInterfaceId = "$.ifid";
        private const string JsonContentType = "application/json";

        private const string ModelDiscoveryInterfaceId = "urn:azureiot:ModelDiscovery:ModelInformation:1";
        private const string ModelDiscoveryInterfaceInstanceName = "urn:azureiot:ModelDiscovery:ModelInformation";
        private const string ModelInformationSchema = "modelInformation";

        private const string JsonCommandRequestId = "commandRequest.requestId";
        private const string JsonCommandRequestValue = "commandRequest.value";

        private readonly DeviceClient deviceClient;
        private Dictionary<string, DigitalTwinInterface> interfaces = new Dictionary<string, DigitalTwinInterface>();
        private readonly DigitalTwinBindingFormatterCollection digitalTwinFormatterCollection;

        public DigitalTwinClient(DeviceClient deviceClient)
        {
            this.deviceClient = deviceClient;
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
                interfaces.Add(dtInterface.InstanceName, dtInterface);
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
            await SetupDigitalTwinClientAsync().ConfigureAwait(false);
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

        internal async Task SendTelemetryAsync(string interfaceId, string interfaceInstanceName, DigitalTwinProperty telemetryValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            await deviceClient.SendEventAsync(CreateDigitalTwinMessage(interfaceId, interfaceInstanceName, telemetryValue), cancellationToken).ConfigureAwait(false);
        }

        internal async Task UpdateAsyncCommandStatusAsync(string interfaceId, string instanceName, DigitalTwinAsyncCommandUpdate update, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        private async Task SetupDigitalTwinClientAsync()
        {
            await deviceClient.SetMethodDefaultHandlerAsync(GenericMethodHandlerAsync, this).ConfigureAwait(false);
            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(GenericPropertyUpdateHandlerAsync, this).ConfigureAwait(false);
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
            message.Properties.Add(IoTHubInterfaceId, ModelDiscoveryInterfaceId);
            message.Properties.Add(IothubInterfaceInstance, ModelDiscoveryInterfaceInstanceName);
            message.ContentType = JsonContentType;
            message.MessageSchema = ModelInformationSchema;
            return message;
        }

        private Message CreateDigitalTwinMessage(string interfaceId, string interfaceInstanceId, DigitalTwinProperty telemetryValue)
        {
            Message message = new Message(
                Encoding.UTF8.GetBytes(
                    digitalTwinFormatterCollection.FromObject(
                        CreateKeyValueDataCollection(telemetryValue.Name, telemetryValue.RawValue))));
            message.Properties.Add(IothubInterfaceInstance, interfaceInstanceId);
            message.Properties.Add(IoTHubInterfaceId, interfaceId);
            message.ContentType = JsonContentType;
            message.MessageSchema = telemetryValue.Name;
            return message;
        }

        private async Task<MethodResponse> GenericMethodHandlerAsync(MethodRequest methodRequest, object userContext)
        {
            // parse the genericMethodRequest.Name to determine which interface to forward
            // with the use interface reference to trigger the generic callback for the interface

            (string interfaceInstanceName, string methodName) = ParseMethodRequestName(methodRequest.Name);

            if (string.IsNullOrEmpty(interfaceInstanceName) || string.IsNullOrEmpty(methodName))
            {
                return new MethodResponse(404);
            }

            CommandCallback callback = interfaces[interfaceInstanceName].CommandHandler;

            var jsonObj  = JObject.Parse(methodRequest.DataAsJson);
            var commandRequestValue = jsonObj.SelectToken(JsonCommandRequestValue)?.ToString();
            DigitalTwinCommandResponse response = await callback(
                new DigitalTwinCommandRequest(
                    methodName,
                    jsonObj.SelectToken(JsonCommandRequestId)?.ToString(),
                    commandRequestValue != null ? Encoding.UTF8.GetBytes(commandRequestValue) : null),
                    userContext).ConfigureAwait(false);
            
            if (!response.Payload.IsEmpty)
            {
                return new MethodResponse(response.Payload.ToArray(), response.Status);
            }

            return new MethodResponse(response.Status);
        }

        private static (string interfaceInstanceName, string methodName) ParseMethodRequestName(string methodRequestName)
        {
            if (string.CompareOrdinal(InterfacesPrefix, 0, methodRequestName, 0, InterfacesPrefix.Length) != 0)
            {
                return (null, null);
            }

            string[] values = methodRequestName.Substring(InterfacesPrefix.Length).Split("*");
            if (values.Length != 2)
            {
                return (null, null);
            }

            return (values[0], values[1]);
        }

        private async Task GenericPropertyUpdateHandlerAsync(TwinCollection desiredProperties, object userContext)
        {
            JObject jsonObj = JObject.Parse(desiredProperties.ToString());
            int version = (int)jsonObj["$version"];

            foreach (var property in jsonObj)
            {
                if (string.CompareOrdinal(InterfacesPrefix, 0, property.Key, 0, InterfacesPrefix.Length) == 0)
                {
                    string interfaceInstanceName = property.Key.Substring(InterfacesPrefix.Length);
                    PropertyUpdatedCallback callback = interfaces[interfaceInstanceName].PropertyUpdatedHandler;

                    foreach (var childToken in property.Value.Children())
                    {
                        string propertyName = ((JProperty)childToken).Name;
                        Memory<byte> propertyValue = Encoding.UTF8.GetBytes(((JProperty)childToken).Value["value"].ToString());
                        await callback(new DigitalTwinPropertyUpdate(
                            propertyName,
                            version,
                            propertyValue,
                            null),
                            userContext).ConfigureAwait(false);
                    }
                }
            }
        }
        #endregion
    }
}
