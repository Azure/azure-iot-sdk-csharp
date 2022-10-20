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
        [ExpectedException(typeof(InvalidOperationException))]
        public void DeviceClientConnectionString_ExtraModuleId_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionString_MissingEndpoint_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeviceClientConnectionString_MissingDeviceId_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionString_MissingSharedAccessKey_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionString_MissingSharedAccessKeyNameAndKey_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionString_EmptySharedAccessKeyName_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionString_EmptySharedAccessKey_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionString_EmptyDeviceId_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionString_EmptyEndpoint_ExceptionTest()
        {
            string connectionString = "HostName=;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionString_InvalidSharedAccessKey_ExceptionTest()
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
        public void DeviceClientConnectionString_InvalidSharedAccessSignature_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessSignature=INVALID;DeviceId=device1";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void DeviceClientConnectionString_NullConnectionString_ExceptionTest()
        {
            var deviceClient = new IotHubDeviceClient(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeviceClientConnectionString_EmptyConnectionString_ExceptionTest()
        {
            var deviceClient = new IotHubDeviceClient("");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public void DeviceClientConnectionString_X509CertFalse_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;X509Cert=false;DeviceId=device";
            var deviceClient = new IotHubDeviceClient(connectionString);
        }
    }
}
