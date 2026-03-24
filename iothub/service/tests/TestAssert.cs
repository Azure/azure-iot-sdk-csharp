// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.Json.Nodes;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    public static class TestAssert
    {
        private static string FormatInvariant(this string format, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, format, args);
        }

        private static string FormatExpectedActual(string expected, string actual)
        {
            return FormatInvariant("Expected:<{0}>.Actual:<{1}>.", expected, actual);
        }

        public static void AreEqualJson(string expectedJson, string actualJson)
        {
            if (expectedJson == null)
            {
                Assert.IsNull(actualJson, FormatExpectedActual("null", actualJson));
            }

            if (expectedJson.Length == 0)
            {
                Assert.AreEqual(actualJson.Length, 0, FormatExpectedActual("empty string", actualJson));
            }

            JsonNode expectedJObject = JsonNode.Parse(expectedJson);
            JsonNode actualJObject = JsonNode.Parse(actualJson);

            Assert.IsTrue(JsonNode.DeepEquals(expectedJObject, actualJObject), $"The provided json strings are not equivalent. \n {expectedJson} \nvs \n{actualJson}");
        }
    }
}