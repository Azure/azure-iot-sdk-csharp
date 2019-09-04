// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Azure.IoT.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Helper;
using Azure.Iot.DigitalTwin.Device.Model;
using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Azure.Iot.DigitalTwin.Device
{
    /// <summary>
    /// Digital Twin Client binds Digital Twin interfaces handles to the IoTHub transport.
    /// </summary>
    public class DigitalTwinClient
    {
        private const string CapabilityModelIdTag = "capabilityModelId";
        private const string InterfacesTag = "interfaces";
        private const string InterfacesPrefix = "$iotin:";
        private const string IothubInterfaceInstance = "$.ifname";
        private const string IoTHubInterfaceId = "$.ifid";
        private const string JsonContentType = "application/json";

        private const string ModelDiscoveryInterfaceId = "urn:azureiot:ModelDiscovery:ModelInformation:1";
        private const string ModelDiscoveryInterfaceInstanceName = "urn_azureiot_ModelDiscovery_ModelInformation";
        private const string CapabilityReportTelemetryName = "modelInformation";

        private const string JsonCommandRequestId = "commandRequest.requestId";
        private const string JsonCommandRequestValue = "commandRequest.value";

        private readonly DeviceClient deviceClient;
        private readonly IDigitalTwinFormatter digitalTwinFormatter = new DigitalTwinJsonFormatter();

        private Dictionary<string, DigitalTwinInterfaceClient> interfaces = new Dictionary<string, DigitalTwinInterfaceClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinClient"/> class.
        /// </summary>
        /// <param name="deviceClient">the IotHub device client to be associated with. </param>
        public DigitalTwinClient(DeviceClient deviceClient)
        {
            this.deviceClient = deviceClient;
        }

        /// <summary>
        /// Register list of interfaces. This method will replace any previously registered interfaces.
        /// </summary>
        /// <param name="capabilityModelId">The capability model id.</param>
        /// <param name="digitalTwinInterfaces">The list of digital twin interfaces.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task RegisterInterfacesAsync(string capabilityModelId, DigitalTwinInterfaceClient[] digitalTwinInterfaces, CancellationToken cancellationToken)
        {
            GuardHelper.ThrowIfNull(digitalTwinInterfaces, nameof(digitalTwinInterfaces));
            GuardHelper.ThrowIfNullOrWhiteSpace(capabilityModelId, nameof(capabilityModelId));

            SdkInformationInterface sdkInformationInterface = new SdkInformationInterface();

            var interfaceName = new Dictionary<string, string>();
            interfaceName.Add("urn_azureiot_ModelDiscovery_ModelInformation", "urn:azureiot:ModelDiscovery:ModelInformation:1");
            interfaceName.Add(sdkInformationInterface.InstanceName, sdkInformationInterface.Id);

            foreach (var dtInterface in digitalTwinInterfaces)
            {
                GuardHelper.ThrowIfNull(dtInterface, nameof(dtInterface));
                interfaceName.Add(dtInterface.InstanceName, dtInterface.Id);
                this.interfaces.Add(dtInterface.InstanceName, dtInterface);
            }

            Dictionary<string, object> modelInformation = new Dictionary<string, object>();
            modelInformation.Add(CapabilityModelIdTag, capabilityModelId);
            modelInformation.Add(InterfacesTag, new DataCollection(this.digitalTwinFormatter.FromObject(interfaceName)));

            // send register interface
            Message msg = CreateTelemetryMessage(
                    ModelDiscoveryInterfaceId,
                    ModelDiscoveryInterfaceInstanceName,
                    CapabilityReportTelemetryName,
                    this.digitalTwinFormatter.FromObject(CreateKeyValueDataCollection(modelInformation)));
            await this.deviceClient.SendEventAsync(
                msg,
                cancellationToken).ConfigureAwait(false);
            msg.Dispose();

            foreach (var dtInterface in digitalTwinInterfaces)
            {
                dtInterface.Initialize(this);
            }

            sdkInformationInterface.Initialize(this);
            await sdkInformationInterface.SendSdkInformationAsync().ConfigureAwait(false);
            await this.SetupDigitalTwinClientAsync().ConfigureAwait(false);
            await this.GetPropertiesAsync().ConfigureAwait(false);
        }

        /// <summary>
        /// Register list of interfaces. This method will replace any previously registered interfaces.
        /// </summary>
        /// <param name="capabilityModelId">The capability model id.</param>
        /// <param name="digitalTwinInterfaceClients">The list of digital twin interfaces.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task RegisterInterfacesAsync(string capabilityModelId, DigitalTwinInterfaceClient[] digitalTwinInterfaceClients)
        {
            await this.RegisterInterfacesAsync(capabilityModelId, digitalTwinInterfaceClients, CancellationToken.None).ConfigureAwait(false);
        }

        public async Task ReportPropertiesAsync(string instanceName, IEnumerable<DigitalTwinPropertyReport> properties, CancellationToken cancellationToken)
        {
            TwinCollection twinCollection = new TwinCollection();

            foreach (DigitalTwinPropertyReport property in properties)
            {
                JToken jTokenValue = null;
                try
                {
                    jTokenValue = JToken.Parse(property.Value);
                }
                catch (Exception)
                {
                    throw;
                }

                Dictionary<string, object> values = new Dictionary<string, object> { { "value", jTokenValue } };
                if (property.DigitalTwinPropertyResponse != DigitalTwinPropertyResponse.Empty)
                {
                    DigitalTwinPropertyResponse response = property.DigitalTwinPropertyResponse;
                    values.Add("sc", response.StatusCode);
                    values.Add("sd", response.StatusDescription);
                    values.Add("sv", response.RespondVersion);
                }

                twinCollection[property.Name] = values;
            }

            cancellationToken.ThrowIfCancellationRequested();

            await this.deviceClient.UpdateReportedPropertiesAsync(
                CreateKeyValueTwinCollection(InterfacesPrefix + instanceName, twinCollection)).ConfigureAwait(false);
        }

        internal async Task SendTelemetryAsync(string interfaceId, string interfaceInstanceName, string telemetryName, string telemetryValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Message msg = CreateTelemetryMessage(interfaceId, interfaceInstanceName, telemetryName, telemetryValue);
            await this.deviceClient.SendEventAsync(msg, cancellationToken).ConfigureAwait(false);
            msg.Dispose();
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

        private static DataCollection CreateKeyValueDataCollection(IEnumerable<KeyValuePair<string, object>> keyValuePairs)
        {
            DataCollection json = new DataCollection();

            foreach (var keyValuePair in keyValuePairs)
            {
                json[keyValuePair.Key] = keyValuePair.Value;
            }

            return json;
        }

        private static Message CreateTelemetryMessage(string interfaceId, string interfaceInstanceId, string telemetryName, string telemetryValue)
        {
            string content = $"{{ \"{telemetryName}\": {telemetryValue} }}";

            Message message = new Message(Encoding.UTF8.GetBytes(content));
            message.Properties.Add(IothubInterfaceInstance, interfaceInstanceId);
            message.Properties.Add(IoTHubInterfaceId, interfaceId);
            message.ContentType = JsonContentType;
            message.MessageSchema = telemetryName;
            return message;
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

        private async Task SetupDigitalTwinClientAsync()
        {
            await this.deviceClient.SetMethodDefaultHandlerAsync(this.GenericMethodHandlerAsync, this).ConfigureAwait(false);
            await this.deviceClient.SetDesiredPropertyUpdateCallbackAsync(this.GenericPropertyUpdateHandlerAsync, this).ConfigureAwait(false);
        }

        private async Task GetPropertiesAsync()
        {
            Twin twin = await this.deviceClient.GetTwinAsync().ConfigureAwait(false);
            JObject desiredJObject = JObject.Parse(twin.Properties.Desired.ToJson());
            JObject reportedJObject = JObject.Parse(twin.Properties.Reported.ToJson());
            int version = (int)desiredJObject["$version"];
            List<Task> tasks = new List<Task>();

            foreach (var interfaceWithPrefix in desiredJObject)
            {
                if (string.CompareOrdinal(InterfacesPrefix, 0, interfaceWithPrefix.Key, 0, InterfacesPrefix.Length) == 0)
                {
                    string interfaceInstanceName = interfaceWithPrefix.Key.Substring(InterfacesPrefix.Length);

                    foreach (var childToken in interfaceWithPrefix.Value.Children())
                    {
                        string propertyName = ((JProperty)childToken).Name;
                        string desiredPropertyValue = JsonConvert.SerializeObject(((JProperty)childToken).Value["value"]);
                        JToken reportedPropertyValueToken = reportedJObject[interfaceWithPrefix.Key]?[propertyName]?["value"];
                        string reportedPropertyValue = reportedPropertyValueToken != null ? JsonConvert.SerializeObject(reportedPropertyValueToken) : null;

                        tasks.Add(this.interfaces[interfaceInstanceName].OnPropertyUpdated(
                            new DigitalTwinPropertyUpdate(
                            propertyName,
                            version,
                            desiredPropertyValue,
                            reportedPropertyValue)));
                    }
                }
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<MethodResponse> GenericMethodHandlerAsync(MethodRequest methodRequest, object userContext)
        {
            /* parse the genericMethodRequest.InstanceName to determine which interface to forward
               with the use interface reference to trigger the generic callback for the interface */

            (string interfaceInstanceName, string methodName) = ParseMethodRequestName(methodRequest.Name);

            if (string.IsNullOrEmpty(interfaceInstanceName) || string.IsNullOrEmpty(methodName))
            {
                return new MethodResponse(404);
            }

            var jsonObj = JObject.Parse(methodRequest.DataAsJson);
            var commandRequestValue = jsonObj.SelectToken(JsonCommandRequestValue)?.ToString();
            DigitalTwinCommandResponse response = await this.interfaces[interfaceInstanceName].OnCommandRequest(
                new DigitalTwinCommandRequest(
                    methodName,
                    jsonObj.SelectToken(JsonCommandRequestId)?.ToString(),
                    commandRequestValue != null ? commandRequestValue : null)).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.Payload))
            {
                return new MethodResponse(Encoding.UTF8.GetBytes(response.Payload), response.Status);
            }

            return new MethodResponse(response.Status);
        }

        private async Task GenericPropertyUpdateHandlerAsync(TwinCollection desiredProperties, object userContext)
        {
            JObject jsonObj = JObject.Parse(desiredProperties.ToJson());
            int version = (int)jsonObj["$version"];

            foreach (var property in jsonObj)
            {
                if (string.CompareOrdinal(InterfacesPrefix, 0, property.Key, 0, InterfacesPrefix.Length) == 0)
                {
                    string interfaceInstanceName = property.Key.Substring(InterfacesPrefix.Length);

                    foreach (var childToken in property.Value.Children())
                    {
                        string propertyName = ((JProperty)childToken).Name;
                        string propertyValue = JsonConvert.SerializeObject(((JProperty)childToken).Value["value"]);
                        await this.interfaces[interfaceInstanceName].OnPropertyUpdated(
                            new DigitalTwinPropertyUpdate(
                            propertyName,
                            version,
                            propertyValue,
                            null)).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}
