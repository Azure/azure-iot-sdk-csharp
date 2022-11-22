// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class TwinStateTests
    {
        private readonly Dictionary<string, object> _sampleTags = new()
        {
            { "SpeedUnity", "MPH" },
            { "ConsumeUnity", "MPG" },
        };

        private readonly InitialTwinPropertyCollection _sampleDesiredProperties = new()
        {
            Properties =
            {
                ["Brand"] = "NiceCar",
                ["Model"] = "SNC4",
                ["MaxSpeed"] = 200,
                ["MaxConsume"] = 15,
            }
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

        [TestMethod]
        public void TwinStateSucceedOnNull()
        {
            // arrange
            Dictionary<string, object> tags = null;
            InitialTwinPropertyCollection desiredProperties = null;

            // act
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.IsNull(initialTwin.Properties.Desired);
        }

        [TestMethod]
        public void TwinStateSucceedOnTagsWitoutDesiredProperties()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = null;

            // act
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.IsNull(initialTwin.Properties.Desired);
        }

        [TestMethod]
        public void TwinStateSucceedOnDesiredPropertiesWitoutTags()
        {
            // arrange
            Dictionary<string, object> tags = null;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;

            // act
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // assert
            Assert.IsNull(initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.Properties.Desired);
        }

        [TestMethod]
        public void TwinStateSucceedOnDesiredPropertiesAndTags()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;

            // act
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // assert
            Assert.AreEqual(tags, initialTwin.Tags);
            Assert.AreEqual(desiredProperties, initialTwin.Properties.Desired);
        }

        [TestMethod]
        public void TwinStateSucceedOnTagsToJson()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = null;
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // act
            string jsonResult = JsonSerializer.Serialize(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyTagsInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        public void TwinStateSucceedOnDesiredPropertiesToJson()
        {
            // arrange
            Dictionary<string, object> tags = null;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // act
            string jsonResult = JsonSerializer.Serialize(initialTwin);

            // assert
            TestAssert.AreEqualJson(OnlyDesiredPropertiesInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        public void TwinStateSucceedOnToJson()
        {
            // arrange
            Dictionary<string, object> tags = _sampleTags;
            InitialTwinPropertyCollection desiredProperties = _sampleDesiredProperties;
            var initialTwin = new InitialTwinState
            {
                Tags = tags,
                Properties =
                {
                    Desired = desiredProperties,
                },
            };

            // act
            string jsonResult = JsonSerializer.Serialize(initialTwin);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJSON, jsonResult);
        }

        [TestMethod]
        public void TwinStateSucceedOnFromJson()
        {
            // act
            InitialTwinState initialTwin = JsonSerializer.Deserialize<InitialTwinState>(FullInitialTwinJSON);

            // assert
            TestAssert.AreEqualJson(FullInitialTwinJSON, JsonSerializer.Serialize(initialTwin));
        }
    }
}
