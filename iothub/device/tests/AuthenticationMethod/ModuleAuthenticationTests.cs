// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.AuthenticationMethod
{
    using System;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.ApiTest;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    public class ModuleAuthenticationMethodTests
    {
        static string fakeConnectionString = "HostName=acme.azure-devices.net;DeviceId=dumpy;ModuleId=dummyModuleId;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
        //Time for expiring this token was generated manually to expire in 2099/12/31
        //{SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=6VoSOEsiHqq6F3%2FL6EVxR1cVxcIMmvFjVWum2S12qHw%3D&se=4102358400}
        static string fakeToken = "HostName=acme.azure-devices.net;CredentialScope=Module;DeviceId=AngelodTest;ModuleID=AngeloModule;SharedAccessSignature=SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=6VoSOEsiHqq6F3%2FL6EVxR1cVxcIMmvFjVWum2S12qHw%3D&se=4102358400";


        [TestMethod]
        [TestCategory("DeviceClient")]
        public void DeviceClient_ModuleAuthenticationWithRegistrySymmetricKey_Test()
        {
            //Arrange
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(fakeConnectionString);

            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNotNull(iotHubConnectionStringBuilder.ModuleId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithRegistrySymmetricKey);

            //Act
            iotHubConnectionStringBuilder.AuthenticationMethod = new ModuleAuthenticationWithRegistrySymmetricKey("Device1", "Module1", "xuh/8amoTRLK6Rt+OQerx2qrRY8wvo2DudannLDqNvE=");

            //Assert
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithRegistrySymmetricKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.AreEqual("xuh/8amoTRLK6Rt+OQerx2qrRY8wvo2DudannLDqNvE=", iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.AreEqual("Device1", iotHubConnectionStringBuilder.DeviceId);
            Assert.AreEqual("Module1", iotHubConnectionStringBuilder.ModuleId);            
        }

        [TestMethod]
        [TestCategory("DeviceClient")]
        public void DeviceClient_ModuleAuthenticationWithToken_Test()
        {
            //Arrange
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(fakeToken);

            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.DeviceId);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithToken);


            //Act
           //{SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=6VoSOEsiHqq6F3%2FL6EVxR1cVxcIMmvFjVWum2S12qHw%3D&se=4102358400}
            iotHubConnectionStringBuilder.AuthenticationMethod = new ModuleAuthenticationWithToken("Device1", "Module1", "SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=6VoSOEsiHqq6F3%2FL6EVxR1cVxcIMmvFjVWum2S12qHw%3D&se=4102358400");

            //Assert
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ModuleAuthenticationWithToken);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.AreEqual("SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=6VoSOEsiHqq6F3%2FL6EVxR1cVxcIMmvFjVWum2S12qHw%3D&se=4102358400", iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.AreEqual("Device1", iotHubConnectionStringBuilder.DeviceId);
            Assert.AreEqual("Module1", iotHubConnectionStringBuilder.ModuleId);
        }
    }
}

