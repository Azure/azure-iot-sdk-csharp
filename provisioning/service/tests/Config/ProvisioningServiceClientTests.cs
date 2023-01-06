// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System.Linq;
using System;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests.Config
{
    [TestClass]
    [TestCategory("Unit")]
    public class ProvisioningServiceClientTests
    {
        [TestMethod]
        public void ProvisioningServiceClient_DefaultMaxDepth()
        {
            // arrange
            string fakeConnectionString = "HostName=acme-dps.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = ProvisioningServiceClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            var defaultSettings = JsonSerializerSettingsInitializer.GetDefaultJsonSerializerSettings();
            Assert.AreEqual(defaultSettings.MaxDepth, 128);
        }

        [TestMethod]
        [ExpectedException(typeof(Newtonsoft.Json.JsonReaderException))]
        public void ProvisioningServiceClientJson_OverrideDefaultJsonSerializer_ExceedMaxDepthThrows()
        {
            // arrange
            string fakeConnectionString = "HostName=acme-dps.azure-devices-provisioning.net;SharedAccessKeyName=provisioningserviceowner;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var deviceClient = ProvisioningServiceClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            //Create a string representation of a nested object (JSON serialized)
            int nRep = 3;
            string json = string.Concat(Enumerable.Repeat("{a:", nRep)) + "1" +
            string.Concat(Enumerable.Repeat("}", nRep));

            var settings = new JsonSerializerSettings { MaxDepth = 2 };
            //// deserialize
            // act
            JsonConvert.DeserializeObject(json, settings);
        }
    }
}
