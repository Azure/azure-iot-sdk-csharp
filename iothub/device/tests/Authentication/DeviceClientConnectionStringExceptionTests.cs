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
        private const string HostName = "acme.azure-devices.net";
        private const string DeviceId = "device1";
        private const string ModuleId = "moduleId";
        private const string SharedAccessKeyName = "AllAccessKey";
        private const string SharedAccessKey = "dGVzdFN0cmluZzE=";

        [TestMethod]
        public async Task DeviceClientConnectionString_ExtraModuleId_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId={DeviceId};ModuleId={ModuleId};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<InvalidOperationException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingEndpoint_ExceptionTest()
        {
            string connectionString = $"DeviceId={DeviceId};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingDeviceId_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};SharedAccessKeyName={SharedAccessKeyName};SharedAccessKey={SharedAccessKey}";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingSharedAccessKey_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKeyName={SharedAccessKeyName}";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_MissingSharedAccessKeyNameAndKey_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId={DeviceId}";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptySharedAccessKeyName_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKeyName=;SharedAccessKey={SharedAccessKey}";;
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptySharedAccessKey_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey="; ;
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptyDeviceId_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId=;SharedAccessKey={SharedAccessKey}"; ;
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_EmptyEndpoint_ExceptionTest()
        {
            string connectionString = $"HostName=;DeviceId={DeviceId};SharedAccessKey={SharedAccessKey}"; ;
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }

        [TestMethod]
        public async Task DeviceClientConnectionString_InvalidSharedAccessKey_ExceptionTest()
        {
            string connectionString = $"HostName={HostName};DeviceId={DeviceId};SharedAccessKey=INVALID";
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
            string connectionString = $"HostName={HostName};SharedAccessSignature=INVALID;DeviceId={DeviceId}";
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
            string connectionString = $"HostName={HostName};X509Cert=false;DeviceId={DeviceId}";
            Func<Task> act = async () => { await using var deviceClient = new IotHubDeviceClient(connectionString); };
            await act.Should().ThrowAsync<FormatException>();
        }
    }
}
