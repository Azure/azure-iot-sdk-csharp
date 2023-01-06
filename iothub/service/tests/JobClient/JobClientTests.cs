// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Azure.Devices;
using FluentAssertions;
using Newtonsoft.Json;
using System.Linq;

namespace Microsoft.Azure.Devices
{
    [TestClass]
    [TestCategory("Unit")]
    public class JobClientTests
    {
        [TestMethod]
        public void JobClient_DefaultMaxDepth()
        {
            // arrange
            string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var jobClient = JobClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            var defaultSettings = JsonSerializerSettingsInitializer.GetDefaultJsonSerializerSettings();
            defaultSettings.MaxDepth.Should().Be(128);
        }

        [TestMethod]
        public void JobClient_Json_OverrideDefaultJsonSerializer_ExceedMaxDepthThrows()
        {
            // arrange
            string fakeConnectionString = "HostName=acme.azure-devices.net;SharedAccessKeyName=AllAccessKey;SharedAccessKey=dGVzdFN0cmluZzE=";
            using var jobClient = JobClient.CreateFromConnectionString(fakeConnectionString);
            // above arragement is only for setting the defaultJsonSerializerSettings

            //Create a string representation of a nested object (JSON serialized)
            int nRep = 3;
            string json = string.Concat(Enumerable.Repeat("{a:", nRep)) + "1" +
            string.Concat(Enumerable.Repeat("}", nRep));

            var settings = new JsonSerializerSettings { MaxDepth = 2 };
            //// deserialize
            // act
            Func<object> act = () => JsonConvert.DeserializeObject(json, settings);

            // assert
            act.Should().Throw<Newtonsoft.Json.JsonReaderException>();
        }
    }
}
