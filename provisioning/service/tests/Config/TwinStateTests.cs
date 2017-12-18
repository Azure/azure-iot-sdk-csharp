// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class TwinStateTests
    {
        private TwinCollection SampleTags = new TwinCollection
        {
            ["SpeedUnity"] = "MPH",
            ["ConsumeUnity"] = "MPG",
        };

        private TwinCollection SampleDesiredProperties = new TwinCollection
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
        [TestCategory("DevService")]
        public void TwinState_SucceedOnNull()
        {
            // arrange
            TwinCollection tags = null;
            TwinCollection desiredProperties = null;

            // act
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.IsNull(initialTwin.DesiredProperties);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnTagsWitoutDesiredProperties()
        {
            // arrange
            TwinCollection tags = SampleTags;
            TwinCollection desiredProperties = null;

            // act
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.IsNull(initialTwin.DesiredProperties);
        }

        /* SRS_TWIN_STATE_21_003: [The get.DesiredProperties shall return the content of _properties.Desired.] */
        /* SRS_TWIN_STATE_21_005: [The set.DesiredProperties shall convert the provided value in a 
                                    TwinPropertyes.Desired and store it as _properties.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnDesiredPropertiesWitoutTags()
        {
            // arrange
            TwinCollection tags = null;
            TwinCollection desiredProperties = SampleDesiredProperties;

            // act
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.DesiredProperties);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnDesiredPropertiesAndTags()
        {
            // arrange
            TwinCollection tags = SampleTags;
            TwinCollection desiredProperties = SampleDesiredProperties;

            // act
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.DesiredProperties);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnTagsToJson()
        {
            // arrange
            TwinCollection tags = SampleTags;
            TwinCollection desiredProperties = null;
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyTagsInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnDesiredPropertiesToJson()
        {
            // arrange
            TwinCollection tags = null;
            TwinCollection desiredProperties = SampleDesiredProperties;
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyDesiredPropertiesInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnToJson()
        {
            // arrange
            TwinCollection tags = SampleTags;
            TwinCollection desiredProperties = SampleDesiredProperties;
            TwinState initialTwin = new TwinState(tags, desiredProperties);

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void TwinState_SucceedOnFromJson()
        {
            // arrange
            TwinCollection tags = SampleTags;
            TwinCollection desiredProperties = SampleDesiredProperties;

            // act
            TwinState initialTwin = JsonConvert.DeserializeObject<TwinState>(FullInitialTwinJSON);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJSON, JsonConvert.SerializeObject(initialTwin));
        }
    }
}
