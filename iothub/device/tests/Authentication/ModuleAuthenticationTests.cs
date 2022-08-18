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
        static string fakeHostName = "acme.azure-devices.net";
        static string fakeToken = "HostName=acme.azure-devices.net;CredentialScope=Module;DeviceId=AngelodTest;ModuleID=AngeloModule;SharedAccessSignature=SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400";


        [TestMethod]
        public void IotHubDeviceClient_ModuleAuthenticationWithRegistrySymmetricKey_Test()
        {
            //Arrange
            var iotHubConnectionStringBuilder = new IotHubConnectionCredentials(fakeConnectionString);

            // Assert
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNotNull(iotHubConnectionStringBuilder.ModuleId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithRegistrySymmetricKey);

            //Act
            iotHubConnectionStringBuilder = new IotHubConnectionCredentials(
                new ModuleAuthenticationWithRegistrySymmetricKey("Device1", "Module1", "dGVzdFN0cmluZzM="),
                fakeHostName);

            //Assert
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithRegistrySymmetricKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.AreEqual("dGVzdFN0cmluZzM=", iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.AreEqual("Device1", iotHubConnectionStringBuilder.DeviceId);
            Assert.AreEqual("Module1", iotHubConnectionStringBuilder.ModuleId);
        }

        [TestMethod]
        public void IotHubDeviceClient_ModuleAuthenticationWithToken_Test()
        {
            //Arrange
            var iotHubConnectionStringBuilder = new IotHubConnectionCredentials(fakeToken);

            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithToken);


            //Act
            iotHubConnectionStringBuilder = new IotHubConnectionCredentials(
                new ModuleAuthenticationWithToken("Device1", "Module1", "SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400"),
                fakeHostName);

            //Assert
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithToken);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.AreEqual("SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400", iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.AreEqual("Device1", iotHubConnectionStringBuilder.DeviceId);
            Assert.AreEqual("Module1", iotHubConnectionStringBuilder.ModuleId);
        }
    }
}

