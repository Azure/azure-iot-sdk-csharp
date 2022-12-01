// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices.Client;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientConnectionStringExceptionTests
    {
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DeviceClientConnectionString_ExtraModuleId_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public async Task DeviceClientConnectionString_MissingEndpoint_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClientConnectionString_MissingDeviceId_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public async Task DeviceClientConnectionString_MissingSharedAccessKey_ExceptionTest()
        {
            string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public async Task DeviceClientConnectionString_MissingSharedAccessKeyNameAndKey_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceClientConnectionString_EmptySharedAccessKeyName_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceClientConnectionString_EmptySharedAccessKey_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceClientConnectionString_EmptyDeviceId_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public async Task DeviceClientConnectionString_EmptyEndpoint_ExceptionTest()
        {
            string connectionString = "HostName=;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public async Task DeviceClientConnectionString_InvalidSharedAccessKey_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=INVALID;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        public async Task DeviceClientMalformedConnectionStringTest()
        {
            string connectionString = "TODO: IoT hub connection string to connect to";
            try
            {
                await using var deviceClient = new IotHubDeviceClient(connectionString);
            }
            catch (FormatException fe)
            {
                Assert.IsTrue(fe.Message.Contains("Malformed Token"), "Exception should mention 'Malformed Token' Actual :" + fe.Message);
            }
        }

        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public async Task DeviceClientConnectionString_InvalidSharedAccessSignature_ExceptionTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessSignature=INVALID;DeviceId=device1";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public async Task DeviceClientConnectionString_NullConnectionString_ExceptionTest()
        {
            await using var deviceClient = new IotHubDeviceClient(null);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public async Task DeviceClientConnectionString_EmptyConnectionString_ExceptionTest()
        {
            await using var deviceClient = new IotHubDeviceClient("");
        }

        [TestMethod]
        [ExpectedException(typeof(FormatException))]
        public async Task DeviceClientConnectionString_X509CertFalse_Test()
        {
            string connectionString = "HostName=acme.azure-devices.net;X509Cert=false;DeviceId=device";
            await using var deviceClient = new IotHubDeviceClient(connectionString);
        }
    }
}
