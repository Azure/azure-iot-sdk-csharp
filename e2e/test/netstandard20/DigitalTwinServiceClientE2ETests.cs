// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.IoT.DigitalTwin.Service;
using Azure.IoT.DigitalTwin.Service.Models;
using Microsoft.Rest;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DigitalTwin = Azure.IoT.DigitalTwin.Service.Models.DigitalTwin;

namespace Microsoft.Azure.Devices.E2ETests
{
    [TestClass]
    [TestCategory("IoTHub-E2E")]
    public class DigitalTwinServiceClientE2ETests
    {
        
        private readonly string DevicePrefix = $"E2E_{nameof(ServiceClientE2ETests)}_";
        private static TestLogging _log = TestLogging.GetInstance();

        private const string digitalTwinPrefix = "digitalTwin-";

        private readonly ConsoleEventListener _listener;

        public DigitalTwinServiceClientE2ETests()
        {
            _listener = TestConfig.StartEventListener();
        }

        [TestMethod]
        public async Task testGetAllInterfaces()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);

            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = testDevice.Id;
                DigitalTwin digitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinId).ConfigureAwait(false);

                Assert.AreEqual(1, digitalTwin.Components.Count);
                Assert.AreEqual(1, digitalTwin.Version);
                foreach (var component in digitalTwin.Components)
                {

                }
            }
        }

        [TestMethod]
        public async Task testGetAllInterfacesAsync()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);

            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = testDevice.Id;
                DigitalTwin digitalTwin = await digitalTwinServiceClient.GetDigitalTwinAsync(digitalTwinId).ConfigureAwait(false);

                Assert.AreEqual(1, digitalTwin.Components.Count);
                Assert.AreEqual(1, digitalTwin.Version);
            }
        }

        [TestMethod]
        public async Task testGetSingleInterface()
        {
            //Freshly created devices do not have any digitalTwinComponents, so need to use a pre-existing device for now
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);
            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = "samguo01";
                DigitalTwin digitalTwin = digitalTwinServiceClient.GetDigitalTwinComponent(digitalTwinId, "environmentalsensor");

                Assert.AreEqual(1, digitalTwin.Components.Count);
            }
        }

        [TestMethod]
        public async Task GetModel()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);
            
            object a = digitalTwinServiceClient.GetModel(new ModelId("urn:azureiot:DeviceManagement:DeviceInformation:1"));
            Assert.IsNotNull(a);
        }

        [TestMethod]
        public async Task testUpdateDigitalTwinSingleProperty()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = testDevice.Id;
                string componentName = "sampleDeviceInfo";
                string propertyName = "somePropertyName";
                string propertyValue = "somePropertyValue-" + Guid.NewGuid().ToString();

                DigitalTwin digitalTwin = await digitalTwinServiceClient.UpdateDigitalTwinPropertyAsync(digitalTwinId, componentName, propertyName, propertyValue).ConfigureAwait(false);

                Assert.IsTrue(digitalTwin.Components.ContainsKey(componentName));
                Assert.AreEqual(componentName, digitalTwin.Components[componentName].Name);
                Assert.IsTrue(digitalTwin.Components[componentName].Properties.ContainsKey(propertyName));
                Assert.AreEqual(propertyValue, digitalTwin.Components[componentName].Properties[propertyName].Desired.Value);
            }
        }

        [TestMethod]
        public async Task testUpdateDigitalTwinAsyncWithFullPatch()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = testDevice.Id;
                string componentName = "sampleDeviceInfo";
                string propertyName = "somePropertyName";
                string propertyValue = "somePropertyValue-" + Guid.NewGuid().ToString();
                var value = new DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired(propertyValue);
                DigitalTwinInterfacesPatch patch = new DigitalTwinInterfacesPatch()
                {
                    Interfaces = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValue>
                    {
                        {
                            componentName, new DigitalTwinInterfacesPatchInterfacesValue()
                            {
                                Properties = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValuePropertiesValue>()
                                {
                                    {propertyName, new DigitalTwinInterfacesPatchInterfacesValuePropertiesValue(value)},
                                    {propertyName + "2", new  DigitalTwinInterfacesPatchInterfacesValuePropertiesValue(value)}
                                }
                            }
                        }
                    }
                };

                DigitalTwin digitalTwin = await digitalTwinServiceClient.UpdateDigitalTwinAsync(digitalTwinId, patch).ConfigureAwait(false);

                Assert.IsTrue(digitalTwin.Components.ContainsKey(componentName));
                Assert.AreEqual(componentName, digitalTwin.Components[componentName].Name);
                Assert.IsTrue(digitalTwin.Components[componentName].Properties.ContainsKey(propertyName));
                Assert.AreEqual(propertyValue, digitalTwin.Components[componentName].Properties[propertyName].Desired.Value);
            }
        }
        
        [TestMethod]
        public async Task testUpdateDigitalTwinWithFullPatch()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = testDevice.Id;
                string componentName = "sampleDeviceInfo";
                string propertyName = "somePropertyName";
                string propertyValue = "somePropertyValue-" + Guid.NewGuid().ToString();
                var value = new DigitalTwinInterfacesPatchInterfacesValuePropertiesValueDesired(propertyValue);
                DigitalTwinInterfacesPatch patch = new DigitalTwinInterfacesPatch()
                {
                    Interfaces = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValue>
                    {
                        {
                            componentName, new DigitalTwinInterfacesPatchInterfacesValue()
                            {
                                Properties = new Dictionary<string, DigitalTwinInterfacesPatchInterfacesValuePropertiesValue>()
                                {
                                    {propertyName, new DigitalTwinInterfacesPatchInterfacesValuePropertiesValue(value)},
                                    {propertyName + "2", new  DigitalTwinInterfacesPatchInterfacesValuePropertiesValue(value)}
                                }
                            }
                        }
                    }
                };
                DigitalTwin digitalTwin = digitalTwinServiceClient.UpdateDigitalTwin(digitalTwinId, patch);

                Assert.IsTrue(digitalTwin.Components.ContainsKey(componentName));
                Assert.AreEqual(componentName, digitalTwin.Components[componentName].Name);
                Assert.IsTrue(digitalTwin.Components[componentName].Properties.ContainsKey(propertyName));
                Assert.AreEqual(propertyValue, digitalTwin.Components[componentName].Properties[propertyName].Desired.Value);
            }
        }

        [TestMethod]
        public async Task testInvokeCommandWithByteArray()
        {
            DigitalTwinServiceClient digitalTwinServiceClient = new DigitalTwinServiceClient(Configuration.IoTHub.ConnectionString);
            RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);

            using (TestDevice testDevice = await TestDevice.CreateTestDeviceAsync(digitalTwinPrefix).ConfigureAwait(false))
            {
                string digitalTwinId = testDevice.Id;
                string componentName = "sampleDeviceInfo";
                string commandName = "asdf";
                string json = "deviceId";
                byte[] argument = Encoding.UTF8.GetBytes(json);
                DigitalTwin digitalTwin = new DigitalTwin(null, 10);
                string m = Newtonsoft.Json.JsonConvert.SerializeObject(digitalTwin);
                byte[] digitalTwinArgument = Encoding.UTF8.GetBytes(m);
                await digitalTwinServiceClient.InvokeCommandAsync(digitalTwinId, componentName, commandName, argument).ConfigureAwait(false);
            }
        }
    }
}
