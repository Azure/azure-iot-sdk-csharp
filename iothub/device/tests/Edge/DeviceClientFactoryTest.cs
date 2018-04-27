// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Client.Edge;
using Microsoft.Azure.Devices.Client.Transport.Mqtt;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.Edge
{

    [TestClass]
    public class DeviceClientFactoryTest
    {
        readonly string serverUrl;
        readonly byte[] sasKey = System.Text.Encoding.UTF8.GetBytes("key");
        readonly string iotHubConnectionString;

        const string EdgehubConnectionstringVariableName = "EdgeHubConnectionString";
        const string IotEdgedUriVariableName = "IOTEDGE_IOTEDGEDURI";
        const string IotHubHostnameVariableName = "IOTEDGE_IOTHUBHOSTNAME";
        const string GatewayHostnameVariableName = "IOTEDGE_GATEWAYHOSTNAME";
        const string DeviceIdVariableName = "IOTEDGE_DEVICEID";
        const string ModuleIdVariableName = "IOTEDGE_MODULEID";
        const string AuthSchemeVariableName = "IOTEDGE_AUTHSCHEME";

        public DeviceClientFactoryTest()
        {
            this.serverUrl = "localhost";
            this.iotHubConnectionString = "Hostname=iothub.test;DeviceId=device1;ModuleId=module1;SharedAccessKey=" + Convert.ToBase64String(this.sasKey);
        }

        [TestMethod]
        public void TestCreate_FromConnectionStringEnvironment_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this.iotHubConnectionString);
            DeviceClient dc = new DeviceClientFactory().Create();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromConnectionStringEnvironment_SetTransportType_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this.iotHubConnectionString);
            DeviceClient dc = new DeviceClientFactory(TransportType.Mqtt_Tcp_Only).Create();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromConnectionStringEnvironment_SetTransportSettings_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, this.iotHubConnectionString);
            DeviceClient dc = new DeviceClientFactory(new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) }).Create();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(EdgehubConnectionstringVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_MissingVariable_ShouldThrow()
        {
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this.serverUrl);
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_UnsupportedAuth_ShouldThrow()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this.serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");

            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "x509Cert");
            TestAssert.Throws<InvalidOperationException>(() => new DeviceClientFactory().Create());

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this.serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            DeviceClient dc = new DeviceClientFactory().Create();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_SetTransportType_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this.serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            DeviceClient dc = new DeviceClientFactory(TransportType.Mqtt_Tcp_Only).Create();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }

        [TestMethod]
        public void TestCreate_FromEnvironment_SetTransportSettings_ShouldCreateClient()
        {
            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, this.serverUrl);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, "iothub.test");
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, "localhost");
            Environment.SetEnvironmentVariable(DeviceIdVariableName, "device1");
            Environment.SetEnvironmentVariable(ModuleIdVariableName, "module1");
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, "sasToken");

            DeviceClient dc = new DeviceClientFactory(new ITransportSettings[] { new MqttTransportSettings(TransportType.Mqtt_Tcp_Only) }).Create();

            Assert.IsNotNull(dc);

            Environment.SetEnvironmentVariable(IotEdgedUriVariableName, null);
            Environment.SetEnvironmentVariable(IotHubHostnameVariableName, null);
            Environment.SetEnvironmentVariable(GatewayHostnameVariableName, null);
            Environment.SetEnvironmentVariable(DeviceIdVariableName, null);
            Environment.SetEnvironmentVariable(ModuleIdVariableName, null);
            Environment.SetEnvironmentVariable(AuthSchemeVariableName, null);
        }
    }
}
