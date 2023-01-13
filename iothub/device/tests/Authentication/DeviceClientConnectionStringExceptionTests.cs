// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using FluentAssertions;

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    [TestClass]
    [TestCategory("Unit")]
    public class DeviceClientConnectionStringExceptionTests
    {
        [TestMethod]
        public async Task DeviceClientConnectionString_ExtraModuleId_ExceptionTest()
        {
            const string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;DeviceId=device1;ModuleId=module1;SharedAccessKey=dGVzdFN0cmluZzE=";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingEndpoint_ExceptionTest()
        {
            const string connectionString = "SharedAccessKeyName=AllAccessKey;DeviceId=device1;SharedAccessKey=dGVzdFN0cmluZzE=";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingDeviceId_ExceptionTest()
        {
            const string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingSharedAccessKey_ExceptionTest()
        {
            const string connectionString = "SharedAccessKeyName=AllAccessKey;HostName=acme.azure-devices.net;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingSharedAccessKeyNameAndKey_ExceptionTest()
        {
            const string connectionString = "HostName=acme.azure-devices.net;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptySharedAccessKeyName_ExceptionTest()
        {
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptySharedAccessKey_ExceptionTest()
        {
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptyDeviceId_ExceptionTest()
        {
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptyEndpoint_ExceptionTest()
        {
            const string connectionString = "HostName=;SharedAccessKey=dGVzdFN0cmluZzE=;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_InvalidSharedAccessKey_ExceptionTest()
        {
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessKey=INVALID;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientMalformedConnectionStringTest()
        {
            const string connectionString = "TODO: IoT hub connection string to connect to";
            try
            {
                await using var deviceClient = new IotHubDeviceClient(connectionString);
            }
            catch (FormatException fe)
            {
                fe.Message.Contains("Malformed Token").Should().BeTrue("Exception should mention 'Malformed Token' Actual :" + fe.Message);
            }
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_InvalidSharedAccessSignature_ExceptionTest()
        {
            const string connectionString = "HostName=acme.azure-devices.net;SharedAccessSignature=INVALID;DeviceId=device1";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
         public async Task DeviceClientConnectionString_NullConnectionString_ExceptionTest()
        {
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(null); };
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptyConnectionString_ExceptionTest()
        {
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(""); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_X509CertFalse_Test()
        {
            const string connectionString = "HostName=acme.azure-devices.net;X509Cert=false;DeviceId=device";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }
    }
}
