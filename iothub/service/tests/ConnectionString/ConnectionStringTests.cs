// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Api.Test.ConnectionString
{
    [TestClass]
    [TestCategory("Unit")]
    public class ConnectionStringTests
    {
        [TestMethod]
        public void IotHubConnectionStringBuilderTest()
        {
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            IotHubConnectionString hubCs = IotHubConnectionStringParser.Parse(cs);
            hubCs.HostName.Should().NotBeNull();
            hubCs.SharedAccessKey.Should().NotBeNull();
            hubCs.SharedAccessKeyName.Should().NotBeNull();
            hubCs.SharedAccessSignature.Should().BeNull();

            cs = "HostName=acme.azure-devices.net;CredentialType=SharedAccessSignature;SharedAccessKeyName=AllAccessKey;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";
            hubCs = IotHubConnectionStringParser.Parse(cs);
            hubCs.HostName.Should().NotBeNull();
            hubCs.SharedAccessKey.Should().BeNull();
            hubCs.SharedAccessKeyName.Should().NotBeNull();
            hubCs.SharedAccessSignature.Should().NotBeNull();
        }

        [TestMethod]
        public void IotHubConnectionStringBuildTokenTest()
        {
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            IotHubConnectionString hubCs = IotHubConnectionStringParser.Parse(cs);

            // Builds new SAS under internally
            string password = hubCs.GetAuthorizationHeader();
            password.Should().NotBeNull();
        }

        [TestMethod]
        public async Task IotHubConnectionStringGetTokenAysncTest()
        {
            string cs = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            IotHubConnectionString hubCs = IotHubConnectionStringParser.Parse(cs);

            CbsToken token = await hubCs.GetTokenAsync(null, null, null);
            token.Should().NotBeNull();
        }
    }
}
