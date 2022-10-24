// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TwinStateTests
    {
        private ProvisioningTwinProperties SampleTags = new ProvisioningTwinProperties
        {
            ["SpeedUnity"] = "MPH",
            ["ConsumeUnity"] = "MPG",
        };

        private ProvisioningTwinProperties SampleDesiredProperties = new ProvisioningTwinProperties
        {
            ["Brand"] = "NiceCar",
            ["Model"] = "SNC4",
            ["MaxSpeed"] = 200,
            ["MaxConsume"] = 15
        };

        private const string OnlyTagsInitialTwinJSON =
            "{" +
            "  \"tags\":{" +
            "    \"SpeedUnity\":\"MPH\"," +
            "    \"ConsumeUnity\":\"MPG\"" +
            "  }" +
            "}";

        private const string OnlyDesiredPropertiesInitialTwinJSON =
            "{" +
            "  \"properties\":{" +
            "    \"desired\":{" +
            "      \"Brand\":\"NiceCar\"," +
            "      \"Model\":\"SNC4\"," +
            "      \"MaxSpeed\":200," +
            "      \"MaxConsume\":15" +
            "    }" +
            "  }" +
            "}";

        private const string FullInitialTwinJSON =
            "{" +
            "  \"tags\":{" +
            "    \"SpeedUnity\":\"MPH\"," +
            "    \"ConsumeUnity\":\"MPG\"" +
            "  }," +
            "  \"properties\":{" +
            "    \"desired\":{" +
            "      \"Brand\":\"NiceCar\"," +
            "      \"Model\":\"SNC4\"," +
            "      \"MaxSpeed\":200," +
            "      \"MaxConsume\":15" +
            "    }" +
            "  }" +
            "}";

        /* SRS_TWIN_STATE_21_001: [The constructor shall store the provided tags and desiredProperties.] */
        /* SRS_TWIN_STATE_21_002: [If the _properties is null, the get.DesiredProperties shall return null.] */
        /* SRS_TWIN_STATE_21_004: [If the value is null, the set.DesiredProperties shall set _properties as null.] */
        [TestMethod]
        public void TwinStateSucceedOnNull()
        {
            // arrange
            ProvisioningTwinProperties tags = null;
            ProvisioningTwinProperties desiredProperties = null;

            // act
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.IsNull(initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void TwinStateSucceedOnTagsWitoutDesiredProperties()
        {
            // arrange
            ProvisioningTwinProperties tags = SampleTags;
            ProvisioningTwinProperties desiredProperties = null;

            // act
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.IsNull(initialTwin.DesiredProperties);
        }

        /* SRS_TWIN_STATE_21_003: [The get.DesiredProperties shall return the content of _properties.Desired.] */
        /* SRS_TWIN_STATE_21_005: [The set.DesiredProperties shall convert the provided value in a 
                                    TwinPropertyes.Desired and store it as _properties.] */
        [TestMethod]
        public void TwinStateSucceedOnDesiredPropertiesWitoutTags()
        {
            // arrange
            ProvisioningTwinProperties tags = null;
            ProvisioningTwinProperties desiredProperties = SampleDesiredProperties;

            // act
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void TwinStateSucceedOnDesiredPropertiesAndTags()
        {
            // arrange
            ProvisioningTwinProperties tags = SampleTags;
            ProvisioningTwinProperties desiredProperties = SampleDesiredProperties;

            // act
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void TwinStateSucceedOnTagsToJson()
        {
            // arrange
            ProvisioningTwinProperties tags = SampleTags;
            ProvisioningTwinProperties desiredProperties = null;
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyTagsInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        public void TwinStateSucceedOnDesiredPropertiesToJson()
        {
            // arrange
            ProvisioningTwinProperties tags = null;
            ProvisioningTwinProperties desiredProperties = SampleDesiredProperties;
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyDesiredPropertiesInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        public void TwinStateSucceedOnToJson()
        {
            // arrange
            ProvisioningTwinProperties tags = SampleTags;
            ProvisioningTwinProperties desiredProperties = SampleDesiredProperties;
            var initialTwin = new ProvisioningTwinState(tags, desiredProperties);

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        public void TwinStateSucceedOnFromJson()
        {
            // arrange
            ProvisioningTwinProperties tags = SampleTags;
            ProvisioningTwinProperties desiredProperties = SampleDesiredProperties;

            // act
            ProvisioningTwinState initialTwin = JsonConvert.DeserializeObject<ProvisioningTwinState>(FullInitialTwinJSON);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJSON, JsonConvert.SerializeObject(initialTwin));
        }
    }
}
