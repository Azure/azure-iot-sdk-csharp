// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test.ConnectionString
{
    using System;

    using Microsoft.Azure.Devices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class ServiceClientConnectionStringExceptionTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingEndpointExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyNameExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey"; 
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyNameAndKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringIotHubScopeSharedAccessSignatureCredentialTypeMissingSharedAccessKeyNameExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessSignature;SharedAccessKey=CQN2K33r45/0WeIjpqmErV5EIvX8JZrozt3NEHCEkG8=";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringIotHubScopeSharedAccessSignatureCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessSignature;SharedAccessKeyName=AllAccessKey";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringIotHubScopeSharedAccessKeyCredentialTypeMissingSharedAccessKeyNameAndKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void ServiceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeNotAllowedSharedAccessKeyNameExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey;SharedAccessKeyName=AllAccessKey";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void ServiceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeInvalidSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey;SharedAccessKeyName=blah;SharedAccessKey=INVALID";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void ServiceClientConnectionStringDeviceScopeImplicitSharedAccessKeyCredentialTypeInvalidSharedAccessSignatureExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialScope=IotHub;CredentialType=SharedAccessKey;SharedAccessKeyName=blah;SharedAccessSignature=INVALID";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ServiceClientConnectionStringEmptyConnectionStringExceptionTest()
        {
            string connectionString = "";
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void ServiceClientConnectionStringNullConnectionStringExceptionTest()
        {
            string connectionString = null;
            var serviceClient = ServiceClient.CreateFromConnectionString(connectionString);
        }
    }
}

