// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class RegistrationRequestPayloadTests
    {
        [TestMethod]
        public void RegistrationRequestPayload_SetPayload()
        {
            // arrange

            var customPayload = new CustomType { CustomInt = 4, CustomString = "bar" };
            string body = JsonConvert.SerializeObject(customPayload);

            var registrationRequestPayload = new RegistrationRequestPayload();
            registrationRequestPayload.SetPayload(body);

            // act
            CustomType result = JsonConvert.DeserializeObject<CustomType>(registrationRequestPayload.Payload.ToString());

            // assert

            result.CustomInt.Should().Be(customPayload.CustomInt);
            result.CustomString.Should().Be(customPayload.CustomString);
        }

        private class CustomType
        {
            [JsonProperty("customInt")]
            public int CustomInt { get; set; }

            [JsonProperty("customString")]
            public string CustomString { get; set; }
        }
    }
}
