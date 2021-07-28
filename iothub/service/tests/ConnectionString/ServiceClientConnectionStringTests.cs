﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test.ConnectionString
{
    using System;
    using Microsoft.Azure.Devices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class ServiceClientConnectionStringTests
    {
        private class TestAuthenticationMethod : IAuthenticationMethod
        {
            public virtual IotHubConnectionStringBuilder Populate(IotHubConnectionStringBuilder iotHubConnectionStringBuilder)
            {
                // intentionally set SharedAccessKeyName to null;
                iotHubConnectionStringBuilder.SharedAccessKeyName = null;
                iotHubConnectionStringBuilder.SharedAccessKey = "dGVzdFN0cmluZzE=";
                return iotHubConnectionStringBuilder;
            }
        }

        [TestMethod]
        public void ServiceClientConnectionStringDefaultScopeDefaultCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientConnectionStringIotHubScopeImplicitSharedAccessSignatureCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessSignature;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientConnectionStringIotHubScopeExplicitSharedAccessSignatureCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessSignature;SharedAccessKeyName=AllAccessKey;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientConnectionStringIotHubScopeSharedAccessKeyCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientConnectionStringDeviceScopeImplicitSharedAccessSignatureCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessSignature;SharedAccessKeyName=blah;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientConnectionStringDeviceScopeExplicitSharedAccessSignatureCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessSignature;SharedAccessKeyName=blah;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey;SharedAccessKeyName=blah;SharedAccessKey=dGVzdFN0cmluZzE=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            Assert.IsNotNull(serviceClient.Connection.Credential);
        }

        [TestMethod]
        public void ServiceClientIotHubConnectionStringBuilderTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsNull(iotHubConnectionStringBuilder.GatewayHostName);
            Assert.IsNull(iotHubConnectionStringBuilder.ModuleId);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyKey);

            connectionString = "HostName=acme.azure-devices.net;CredentialType=SharedAccessSignature;SharedAccessKeyName=AllAccessKey;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create(connectionString);
            Assert.IsNotNull(iotHubConnectionStringBuilder.HostName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.AuthenticationMethod);
            Assert.IsNull(iotHubConnectionStringBuilder.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessKeyName);
            Assert.IsNotNull(iotHubConnectionStringBuilder.SharedAccessSignature);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyToken);

            // Hostname without DNS is acceptable for localhost testing.
            iotHubConnectionStringBuilder.HostName = "adshgfvyregferuehfiuehr";

            try
            {
                iotHubConnectionStringBuilder.HostName = "acme.azure-devices.net";
                iotHubConnectionStringBuilder.AuthenticationMethod = new TestAuthenticationMethod();
                Assert.Fail("Expected ArgumentException");
            }
            catch (ArgumentException e)
            {
                Assert.IsTrue(e.Message.Contains("SharedAccessKey"));
            }

            iotHubConnectionStringBuilder.HostName = "acme.azure-devices.net";
            iotHubConnectionStringBuilder.AuthenticationMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("AllAccessKey1", "dGVzdFN0cmluZzE=");
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKeyName == "AllAccessKey1");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "dGVzdFN0cmluZzE=");

            iotHubConnectionStringBuilder.AuthenticationMethod = new ServiceAuthenticationWithSharedAccessPolicyToken("AllAccessKey2", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKeyName == "AllAccessKey2");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);

            IAuthenticationMethod authMethod = new ServiceAuthenticationWithSharedAccessPolicyToken("AllAccess1", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme1.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme1.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKeyName == "AllAccess1");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);

            authMethod = new ServiceAuthenticationWithSharedAccessPolicyKey("AllAccess2", "dGVzdFN0cmluZzI=");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme2.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKeyName == "AllAccess2");
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme2.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "dGVzdFN0cmluZzI=");

            authMethod = AuthenticationMethodFactory.CreateAuthenticationWithSharedAccessPolicyKey("AllAccess2", "dGVzdFN0cmluZzI=");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme2.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyKey);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKeyName == "AllAccess2");
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme2.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == null);
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == "dGVzdFN0cmluZzI=");

            authMethod = AuthenticationMethodFactory.CreateAuthenticationWithSharedAccessPolicyToken("AllAccess1", "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey");
            iotHubConnectionStringBuilder = IotHubConnectionStringBuilder.Create("acme1.azure-devices.net", authMethod);
            Assert.IsTrue(iotHubConnectionStringBuilder.AuthenticationMethod is ServiceAuthenticationWithSharedAccessPolicyToken);
            Assert.IsTrue(iotHubConnectionStringBuilder.HostName == "acme1.azure-devices.net");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKeyName == "AllAccess1");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessSignature == "SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey");
            Assert.IsTrue(iotHubConnectionStringBuilder.SharedAccessKey == null);
        }

        [TestMethod]
        public void ServiceClient_ConnectionString_ModuleIdentity_SharedAccessKeyCredentialType_Test()
        {
            string connectionString = "HostName=testhub.azure-devices-int.net;DeviceId=edgecapabledevice1;ModuleId=testModule;SharedAccessKey=dGVzdFN0cmluZzE=;GatewayHostName=edgehub1.ms.com";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);

            Assert.IsNotNull(serviceClient.Connection);
            IotHubConnectionString iotHubConnectionString = (IotHubConnectionString)serviceClient.Connection.Credential;
            Assert.IsNotNull(iotHubConnectionString);
            Assert.AreEqual("testhub.azure-devices-int.net", iotHubConnectionString.Audience);
            Assert.AreEqual("testhub.azure-devices-int.net", iotHubConnectionString.HostName);
            Assert.AreEqual("dGVzdFN0cmluZzE=", iotHubConnectionString.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionString.GetPassword());
        }
    }
}
