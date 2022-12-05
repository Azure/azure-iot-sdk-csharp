// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class InitialTwinTests
    {
        private readonly Dictionary<string, object> _sampleTags = new()
        {
            { "SpeedUnity", "MPH" },
            { "ConsumeUnity", "MPG" },
        };

        private readonly InitialTwinPropertyCollection _sampleDesiredProperties = new()
        {
            ["Brand"] = "NiceCar",
            ["Model"] = "SNC4",
            ["MaxSpeed"] = 200,
            ["MaxConsume"] = 15
        };

        private const string OnlyTagsInitialTwinJson =
            "{" +
            "  \"tags\":{" +
            "    \"SpeedUnity\":\"MPH\"," +
            "    \"ConsumeUnity\":\"MPG\"" +
            "  }" +
            "}";

        private const string OnlyDesiredPropertiesInitialTwinJson =
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

        private const string FullInitialTwinJson =
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

        [TestMethod]
        public void InitialTwinSucceedOnNull()
        {
            // arrange
            Dictionary<string, object> tags = null;
            InitialTwinPropertyCollection desiredProperties = null;

            // act
            var initialTwin = new InitialTwin { Tags = tags, DesiredProperties = desiredProperties };

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.IsNull(initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnTagsWitoutDesiredProperties()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = null;

            // act
            var initialTwin = new InitialTwin { Tags = tags, DesiredProperties = desiredProperties };

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.IsNull(initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnDesiredPropertiesWitoutTags()
        {
            // arrange
            Dictionary<string, object> tags = null;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;

            // act
            var initialTwin = new InitialTwin{ Tags = tags, DesiredProperties = desiredProperties };

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnDesiredPropertiesAndTags()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;

            // act
            var initialTwin = new InitialTwin{ Tags = tags, DesiredProperties = desiredProperties };

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.DesiredProperties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnTagsToJson()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = null;
            var initialTwin = new InitialTwin{ Tags = tags, DesiredProperties = desiredProperties };

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyTagsInitialTwinJson, jsonResult);
        }

        [TestMethod]
        public void InitialTwinSucceedOnDesiredPropertiesToJson()
        {
            // arrange
            Dictionary<string, object> tags = null;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;
            var initialTwin = new InitialTwin{ Tags = tags, DesiredProperties = desiredProperties };

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyDesiredPropertiesInitialTwinJson, jsonResult);
        }

        [TestMethod]
        public void InitialTwinSucceedOnToJson()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;
            var initialTwin = new InitialTwin{ Tags = tags, DesiredProperties = desiredProperties };

            // act
            string jsonResult = JsonConvert.SerializeObject(initialTwin);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJson, jsonResult);
        }

        [TestMethod]
        public void InitialTwinSucceedOnFromJson()
        {
            // act
            InitialTwin initialTwin = JsonConvert.DeserializeObject<InitialTwin>(FullInitialTwinJson);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJson, JsonConvert.SerializeObject(initialTwin));
        }
    }
}
