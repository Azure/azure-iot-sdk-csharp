// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Api.Test.ConnectionString
{
    using System;
    using Microsoft.Azure.Devices;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    [TestClass]
    [TestCategory("Unit")]
    public class ConnectionStringTests
    {
        [TestMethod]
        public void IotHubConnectionStringBuilderTest()
        {
            string connectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            var iotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
            Assert.IsNotNull(iotHubConnectionString.HostName);
            Assert.IsNotNull(iotHubConnectionString.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionString.SharedAccessKeyName);
            Assert.IsNull(iotHubConnectionString.SharedAccessSignature);

            connectionString = "HostName=acme.azure-devices.net;CredentialType=SharedAccessSignature;SharedAccessKeyName=AllAccessKey;SharedAccessSignature=SharedAccessSignature sr=dh%3a%2f%2facme.azure-devices.net&sig=dGVzdFN0cmluZzU=&se=87824124985&skn=AllAccessKey";
            iotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
            Assert.IsNotNull(iotHubConnectionString.HostName);
            Assert.IsNull(iotHubConnectionString.SharedAccessKey);
            Assert.IsNotNull(iotHubConnectionString.SharedAccessKeyName);
            Assert.IsNotNull(iotHubConnectionString.SharedAccessSignature);
        }
    }
}
