 // Copyright (c) Microsoft. All rights reserved.
 // Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Test
{
    [TestClass]
    class SerializationTests
    {
        [TestMethod]
        public async Task JsonDateParseHandlingTest()
        {
            var previousDefaultSettings = JsonConvert.DefaultSettings;

            try
            {
                JsonConvert.DefaultSettings = () => new JsonSerializerSettings
                {
                    DateParseHandling = DateParseHandling.DateTimeOffset
                };

                var now = DateTime.Now;

                const string jsonString = @"
{
 ""deviceId"": ""test"",
 ""etag"": ""AAAAAAAAAAM="",
 ""version"": 5,
 ""status"": ""enabled"",
 ""statusUpdateTime"": ""2018-06-29T21:17:08.7759733"",
 ""connectionState"": ""Connected"",
 ""lastActivityTime"": ""2018-06-29T21:17:08.7759733"",
},"; 

                JsonConvert.DeserializeObject<Twin>(jsonString);
            }
            finally
            {
                JsonConvert.DefaultSettings = previousDefaultSettings;
            }
        }
    }
}
