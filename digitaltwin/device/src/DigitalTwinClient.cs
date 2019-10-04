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

        private const string AsynResultSchema = "asyncResult";
        private const string CommandRequestIdProperty = "iothub-command-request-id";
        private const string CommandStatusCodeProperty = "iothub-command-statuscode";
        private const string CommandNameProperty = "iothub-command-name";

        private readonly DeviceClient deviceClient;
        private readonly IDigitalTwinFormatter digitalTwinFormatter = new DigitalTwinJsonFormatter();

        private Dictionary<string, DigitalTwinInterfaceClient> interfaces = new Dictionary<string, DigitalTwinInterfaceClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="DigitalTwinClient"/> class.
        /// </summary>
        /// <param name="deviceClient">the IotHub device client to be associated with. </param>
        public DigitalTwinClient(DeviceClient deviceClient)
        {
            GuardHelper.ThrowIfNull(deviceClient, nameof(deviceClient));
            this.deviceClient = deviceClient;
        }

        /// <summary>
        /// Register list of interfaces. This method will replace any previously registered interfaces.
        /// </summary>
        /// <param name="capabilityModelId">The capability model id.</param>
        /// <param name="digitalTwinInterfaces">The list of digital twin interfaces.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>Task representing the asynchronous operation.</returns>
        public async Task RegisterInterfacesAsync(string capabilityModelId, IEnumerable<DigitalTwinInterfaceClient> digitalTwinInterfaces, CancellationToken cancellationToken = default)
        {
            Logging.Instance.LogVerbose("Starting interface registration.");
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
            using (Message msg = CreateTelemetryMessage(
                    ModelDiscoveryInterfaceId,
                    ModelDiscoveryInterfaceInstanceName,
                    CapabilityReportTelemetryName,
                    this.digitalTwinFormatter.FromObject(CreateKeyValueDataCollection(modelInformation))))
            {
                await this.deviceClient.SendEventAsync(
                    msg,
                    cancellationToken).ConfigureAwait(false);
            }

            Logging.Instance.LogVerbose("Register Interface message sent.");

            sdkInformationInterface.Initialize(this);
            await sdkInformationInterface.SendSdkInformationAsync().ConfigureAwait(false);
            Logging.Instance.LogVerbose("SDK information sent.");

            await this.SetupDigitalTwinClientAsync().ConfigureAwait(false);

            foreach (var dtInterface in digitalTwinInterfaces)
            {
                dtInterface.Initialize(this);
            }

            // Get properties should only be triggered after interfaces are initialized.
            await this.GetPropertiesAsync().ConfigureAwait(false);
            Logging.Instance.LogVerbose("Get Properties completed.");

            Logging.Instance.LogVerbose("Interface registration completed successfully.");
        }

        /// <summary>
        /// Report properties for the specified interface instance.
        /// </summary>
        /// <param name="instanceName">The interface instance name.</param>
        /// <param name="properties">The list of properties to be reported.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the result of the asynchronous operation.</returns>
        internal virtual async Task ReportPropertiesAsync(string instanceName, IEnumerable<DigitalTwinPropertyReport> properties, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GuardHelper.ThrowIfNull(properties, nameof(properties));

            TwinCollection twinCollection = new TwinCollection();

            foreach (DigitalTwinPropertyReport property in properties)
            {
                property.Validate();
                JToken jTokenValue = null;
                jTokenValue = property.Value != null ? JToken.Parse(property.Value) : null;

                Dictionary<string, object> values = new Dictionary<string, object> { { "value", jTokenValue } };
                if (property.DigitalTwinPropertyResponse != default)
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

        internal virtual async Task SendTelemetryAsync(string interfaceId, string interfaceInstanceName, string telemetryName, string telemetryValue, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using (Message msg = CreateTelemetryMessage(interfaceId, interfaceInstanceName, telemetryName, telemetryValue))
            {
                await this.deviceClient.SendEventAsync(msg, cancellationToken).ConfigureAwait(false);
            }
        }

        internal virtual async Task UpdateAsyncCommandStatusAsync(string interfaceId, string interfaceInstanceName, DigitalTwinAsyncCommandUpdate update, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            this.digitalTwinFormatter.FromObject(update);

            using (
            Message msg = CreateTelemetryMessage(
                    interfaceId,
                    interfaceInstanceName,
                    AsynResultSchema,
                    update.Payload))
            {
                msg.Properties.Add(CommandNameProperty, update.Name);
                msg.Properties.Add(CommandRequestIdProperty, update.RequestId);
                msg.Properties.Add(CommandStatusCodeProperty, update.Status.ToString());
                await this.deviceClient.SendEventAsync(
                    msg,
                    cancellationToken).ConfigureAwait(false);
            }
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

            string[] values = methodRequestName.Substring(InterfacesPrefix.Length).Split('*');
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

            List<Task> tasks = new List<Task>();
            if (desiredJObject.ContainsKey("$version"))
            {
                int version = (int)desiredJObject["$version"];

                foreach (var interfaceWithPrefix in desiredJObject)
                {
                    if (string.CompareOrdinal(InterfacesPrefix, 0, interfaceWithPrefix.Key, 0, InterfacesPrefix.Length) == 0)
                    {
                        string interfaceInstanceName = interfaceWithPrefix.Key.Substring(InterfacesPrefix.Length);

                        if (this.interfaces.ContainsKey(interfaceInstanceName))
                        {
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
                        else
                        {
                            Logging.Instance.LogWarning("Interface {$interfaceInstanceName} is not registered. Received property updates will not be processed.");
                        }
                    }
                    else
                    {
                        Logging.Instance.LogWarning("Non-Interface {$interfaceWithPrefix) received will not be processed.");
                    }
                }
            }
            else
            {
                Logging.Instance.LogWarning("Received property updates without version will not be processed.");
            }

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        private async Task<MethodResponse> GenericMethodHandlerAsync(MethodRequest methodRequest, object userContext)
        {
            /* parse the genericMethodRequest.InstanceName to determine which interface to forward
               with the use interface reference to trigger the generic callback for the interface */

            (string interfaceInstanceName, string methodName) = ParseMethodRequestName(methodRequest.Name);

            if (string.IsNullOrEmpty(interfaceInstanceName)
                || !this.interfaces.ContainsKey(interfaceInstanceName)
                || string.IsNullOrEmpty(methodName))
            {
                Logging.Instance.LogWarning("InterfaceName or command name not valid.");
                return new MethodResponse(404);
            }

            var jsonObj = JObject.Parse(methodRequest.DataAsJson);
            var commandRequestValue = jsonObj.SelectToken(JsonCommandRequestValue)?.ToString();
            DigitalTwinCommandResponse response = await this.interfaces[interfaceInstanceName].OnCommandRequest(
                new DigitalTwinCommandRequest(
                    methodName,
                    jsonObj.SelectToken(JsonCommandRequestId)?.ToString(),
                    commandRequestValue)).ConfigureAwait(false);

            if (!string.IsNullOrEmpty(response.Payload))
            {
                Logging.Instance.LogVerbose("Exit with {$response.Status} and payload.");
                return new MethodResponse(Encoding.UTF8.GetBytes(response.Payload), response.Status);
            }

            Logging.Instance.LogVerbose("Exit with {$response.Status}.");
            return new MethodResponse(response.Status);
        }

        private async Task GenericPropertyUpdateHandlerAsync(TwinCollection desiredProperties, object userContext)
        {
            JObject jsonObj = JObject.Parse(desiredProperties.ToJson());

            if (jsonObj.ContainsKey("$version"))
            {
                int version = (int)jsonObj["$version"];

                foreach (var property in jsonObj)
                {
                    if (string.CompareOrdinal(InterfacesPrefix, 0, property.Key, 0, InterfacesPrefix.Length) == 0)
                    {
                        string interfaceInstanceName = property.Key.Substring(InterfacesPrefix.Length);

                        if (this.interfaces.ContainsKey(interfaceInstanceName))
                        {
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
                        else
                        {
                            Logging.Instance.LogWarning("Interface {$interfaceInstanceName} is not registered. Received property updates will not be processed.");
                        }
                    }
                    else
                    {
                        Logging.Instance.LogWarning("Non-Interface {$interfaceWithPrefix) received will not be processed.");
                    }
                }
            }
            else
            {
                Logging.Instance.LogWarning("Received property updates without version will not be processed.");
            }
        }
    }
}
