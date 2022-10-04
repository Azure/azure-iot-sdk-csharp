// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    using System;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Azure.Devices.Client;

    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientConnectionStringExceptionTests
    {
        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingEndpointExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingDeviceIdExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringDefaultScopeDefaultCredentialTypeMissingSharedAccessKeyNameAndKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionStringIotHubScopeSharedAccessSignatureCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1;SharedAccessKeyName=AllAccessKey";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringIotHubScopeSharedAccessKeyCredentialTypeMissingSharedAccessKeyNameAndKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeMissingSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;CredentialType=SharedAccessKey;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionStringDeviceScopeSharedAccessKeyCredentialTypeNotAllowedSharedAccessKeyNameExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionStringDeviceScopeSharedAccessSignatureCredentialTypeInvalidSharedAccessKeyExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=INVALID;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        public void DeviceClientMalformedConnectionStringTest()
        {
            string connectionString = "TODO: IoT hub connection string to connect to";
            try
            {
                var deviceClient = new IotHubDeviceClient(connectionString);
            }
            catch (FormatException fe)
            {
                Assert.IsTrue(fe.Message.Contains("Malformed Token"), "Exception should mention 'Malformed Token' Actual :" + fe.Message);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionStringDeviceScopeImplicitSharedAccessSignatureCredentialTypeInvalidSharedAccessSignatureExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessSignature=INVALID;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeviceClientConnectionStringNullConnectionStringExceptionTest()
        {
            var deviceClient = new IotHubDeviceClient(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionStringEmptyConnectionStringExceptionTest()
        {
            var deviceClient = new IotHubDeviceClient("");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionStringX509CertFalseTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;X509Cert=false;DeviceId=device";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }
    }
}
