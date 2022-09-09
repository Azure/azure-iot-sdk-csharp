// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.AuthenticationMethod
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.ApiTest;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class ModuleAuthenticationMethodTests
    {
        static string fakeConnectionString = "HostName=acme.azure-devices.net;DeviceId=dumpy;ModuleId=dummyModuleId;SharedAccessKey=dGVzdFN0cmluZzE=";
        static string fakeToken = "HostName=acme.azure-devices.net;CredentialScope=Module;DeviceId=AngelodTest;ModuleID=AngeloModule;SharedAccessSignature=SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400";
        static string fakeHostName = "acme.azure-devices.net";

        [TestMethod]
        public void IotHubDeviceClient_ModuleAuthenticationWithRegistrySymmetricKey_Test()
        {
            //Arrange
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(fakeConnectionString);

            //Assert
            Assert.IsNotNull(iotHubConnectionCredentials.IotHubHostName);
            Assert.IsNotNull(iotHubConnectionCredentials.DeviceId);
            Assert.IsNotNull(iotHubConnectionCredentials.ModuleId);
            Assert.IsNull(iotHubConnectionCredentials.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionCredentials.HostName);
            Assert.IsNotNull(iotHubConnectionCredentials.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionCredentials.SharedAccessKey);
            Assert.IsNull(iotHubConnectionCredentials.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionCredentials.AuthenticationMethod is ModuleAuthenticationWithRegistrySymmetricKey);

            //Act
            iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ModuleAuthenticationWithRegistrySymmetricKey("Device1", "Module1", "dGVzdFN0cmluZzM="),
                fakeHostName);

            //Assert
            Assert.IsTrue(iotHubConnectionCredentials.AuthenticationMethod is ModuleAuthenticationWithRegistrySymmetricKey);
            Assert.IsNull(iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual("dGVzdFN0cmluZzM=", iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual("Device1", iotHubConnectionCredentials.DeviceId);
            Assert.AreEqual("Module1", iotHubConnectionCredentials.ModuleId);
        }

        [TestMethod]
        public void IotHubDeviceClient_ModuleAuthenticationWithToken_Test()
        {
            //Arrange
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(fakeToken);

            //Assert
            Assert.IsNotNull(iotHubConnectionCredentials.IotHubHostName);
            Assert.IsNotNull(iotHubConnectionCredentials.DeviceId);
            Assert.IsNull(iotHubConnectionCredentials.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionCredentials.HostName);
            Assert.IsNotNull(iotHubConnectionCredentials.AuthenticationMethod);
            Assert.IsNull(iotHubConnectionCredentials.SharedAccessKey);
            Assert.IsNull(iotHubConnectionCredentials.SharedAccessKeyName);
            Assert.IsNotNull(iotHubConnectionCredentials.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionCredentials.AuthenticationMethod is ModuleAuthenticationWithToken);


            //Act
            iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ModuleAuthenticationWithToken("Device1", "Module1", "SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400"),
                fakeHostName);

            //Assert
            Assert.IsTrue(iotHubConnectionCredentials.AuthenticationMethod is ModuleAuthenticationWithToken);
            Assert.IsNull(iotHubConnectionCredentials.SharedAccessKey);
            Assert.AreEqual("SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400", iotHubConnectionCredentials.SharedAccessSignature);
            Assert.AreEqual("Device1", iotHubConnectionCredentials.DeviceId);
            Assert.AreEqual("Module1", iotHubConnectionCredentials.ModuleId);
        }
    }
}

