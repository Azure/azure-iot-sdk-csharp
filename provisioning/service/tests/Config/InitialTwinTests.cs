// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class InitialTwinTests
    {
        private readonly JsonDictionary _sampleTags = new()
        {
            { "SpeedUnity", "MPH" },
            { "ConsumeUnity", "MPG" },
        };

        private readonly InitialTwinProperties _sampleDesiredProperties = new()
        {
            Desired = new()
            {
                ["Brand"] = "NiceCar",
                ["Model"] = "SNC4",
                ["MaxSpeed"] = 200,
                ["MaxConsume"] = 15
            }
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
            JsonDictionary tags = null;
            InitialTwinProperties desiredProperties = null;

            // act
            var initialTwin = new InitialTwin { Tags = tags, Properties = desiredProperties };

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.IsNull(initialTwin.Properties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnTagsWitoutDesiredProperties()
        {
            // arrange
            JsonDictionary tags = _sampleTags;
            InitialTwinProperties desiredProperties = null;

            // act
            var initialTwin = new InitialTwin { Tags = tags, Properties = desiredProperties };

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.IsNull(initialTwin.Properties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnDesiredPropertiesWitoutTags()
        {
            // arrange
            JsonDictionary tags = null;
            InitialTwinProperties desiredProperties = _sampleDesiredProperties;

            // act
            var initialTwin = new InitialTwin{ Tags = tags, Properties = desiredProperties };

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.Properties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnDesiredPropertiesAndTags()
        {
            // arrange
            JsonDictionary tags = _sampleTags;
            InitialTwinProperties desiredProperties = _sampleDesiredProperties;

            // act
            var initialTwin = new InitialTwin{ Tags = tags, Properties = desiredProperties };

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.Properties);
        }

        [TestMethod]
        public void InitialTwinSucceedOnTagsToJson()
        {
            // arrange
            JsonDictionary tags = _sampleTags;
            InitialTwinProperties desiredProperties = null;
            var initialTwin = new InitialTwin{ Tags = tags, Properties = desiredProperties };

            // act
            string jsonResult = JsonSerializer.Serialize(initialTwin, JsonSerializerSettings.Options);

            // assert
            TestAssert.AreEqualJson(OnlyTagsInitialTwinJson, jsonResult);
        }

        [TestMethod]
        public void InitialTwinSucceedOnDesiredPropertiesToJson()
        {
            // arrange
            JsonDictionary tags = null;
            InitialTwinProperties desiredProperties = _sampleDesiredProperties;
            var initialTwin = new InitialTwin{ Tags = tags, Properties = desiredProperties };

            // act
            string jsonResult = JsonSerializer.Serialize(initialTwin, JsonSerializerSettings.Options);

            // assert
            TestAssert.AreEqualJson(OnlyDesiredPropertiesInitialTwinJson, jsonResult);
        }

        [TestMethod]
        public void InitialTwinSucceedOnToJson()
        {
            // arrange
            JsonDictionary tags = _sampleTags;
            InitialTwinProperties desiredProperties = _sampleDesiredProperties;
            var initialTwin = new InitialTwin{ Tags = tags, Properties = desiredProperties };

            // act
            string jsonResult = JsonSerializer.Serialize(initialTwin, JsonSerializerSettings.Options);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJson, jsonResult);
        }

        [TestMethod]
        public void InitialTwinSucceedOnFromJson()
        {
            // act
            InitialTwin initialTwin = JsonSerializer.Deserialize<InitialTwin>(FullInitialTwinJson, JsonSerializerSettings.Options);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJson, JsonSerializer.Serialize(initialTwin, JsonSerializerSettings.Options));
        }
    }
}
