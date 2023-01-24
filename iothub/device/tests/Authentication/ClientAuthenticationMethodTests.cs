// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.AuthenticationMethod
{
    [TestClass]
    [TestCategory("Unit")]
    public class ClientAuthenticationMethodTests
    {
        private static readonly string fakeHostName = "acme.azure-devices.net";
        private static readonly string fakeModuleId = "dummyModuleId";
        private static readonly string fakeDeviceId = "dummyDeviceId";
        private static readonly string fakeSharedAccessKey = "dGVzdFN0cmluZzE=";
        private static readonly string fakeConnectionString = $"HostName={fakeHostName};DeviceId={fakeDeviceId};ModuleId={fakeModuleId};SharedAccessKey={fakeSharedAccessKey}";
        private static readonly string fakeToken = $"HostName={fakeHostName};CredentialScope=Module;DeviceId={fakeDeviceId};ModuleID={fakeModuleId};SharedAccessSignature=SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2F{fakeDeviceId}%2Fmodules%2F{fakeModuleId}&sig=dGVzdFN0cmluZzY=&se=4102358400";

        [TestMethod]
        public void IotHubDeviceClient_ClientAuthenticationWithRegistrySymmetricKey_Test()
        {
            //Arrange
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(fakeConnectionString);

            //Assert
            iotHubConnectionCredentials.IotHubHostName.Should().NotBeNull();
            iotHubConnectionCredentials.DeviceId.Should().NotBeNull();
            iotHubConnectionCredentials.ModuleId.Should().NotBeNull();
            iotHubConnectionCredentials.GatewayHostName.Should().BeNull();
            iotHubConnectionCredentials.HostName.Should().NotBeNull();
            iotHubConnectionCredentials.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionCredentials.SharedAccessKey.Should().NotBeNull();
            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessKeyRefresh>();

            //Act
            iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ClientAuthenticationWithSharedAccessKeyRefresh(
                    sharedAccessKey: "dGVzdFN0cmluZzM=",
                    deviceId: "Device1",
                    moduleId: "Module1"),
                fakeHostName);

            //Assert
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessKeyRefresh>();
            iotHubConnectionCredentials.SharedAccessSignature.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKey.Should().Be("dGVzdFN0cmluZzM=");
            iotHubConnectionCredentials.DeviceId.Should().Be("Device1");
            iotHubConnectionCredentials.ModuleId.Should().Be("Module1");
        }

        [TestMethod]
        public void IotHubDeviceClient_ClientAuthenticationWithToken_Test()
        {
            //Arrange
            var iotHubConnectionCredentials = new IotHubConnectionCredentials(fakeToken);

            //Assert
            iotHubConnectionCredentials.IotHubHostName.Should().NotBeNull();
            iotHubConnectionCredentials.DeviceId.Should().NotBeNull();
            iotHubConnectionCredentials.GatewayHostName.Should().BeNull();
            iotHubConnectionCredentials.HostName.Should().NotBeNull();
            iotHubConnectionCredentials.AuthenticationMethod.Should().NotBeNull();
            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessKeyName.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessSignature.Should().NotBeNull();
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessSignature>();


            //Act
            iotHubConnectionCredentials = new IotHubConnectionCredentials(
                new ClientAuthenticationWithSharedAccessSignature("SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400", "Device1", "Module1"),
                fakeHostName);

            //Assert
            iotHubConnectionCredentials.AuthenticationMethod.Should().BeOfType<ClientAuthenticationWithSharedAccessSignature>();
            iotHubConnectionCredentials.SharedAccessKey.Should().BeNull();
            iotHubConnectionCredentials.SharedAccessSignature.Should().Be("SharedAccessSignature sr=iot-edge-1003.private.azure-devices-int.net%2Fdevices%2FAngelodTest%2Fmodules%2FAngeloModule&sig=dGVzdFN0cmluZzY=&se=4102358400");
            iotHubConnectionCredentials.DeviceId.Should().Be("Device1");
            iotHubConnectionCredentials.ModuleId.Should().Be("Module1");
        }
    }
}

