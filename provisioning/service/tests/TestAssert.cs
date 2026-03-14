// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Service.Tests
{
    using System;
    using System.Globalization;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    public static class TestAssert
    {
        public static TException Throws<TException>(Action action, string errorMessage = null) where TException : Exception
        {
            errorMessage = errorMessage ?? "Failed";
            try
            {
                action();
            }
            catch (TException ex)
            {
                return ex;
            }
            catch (Exception ex)
            {
                throw new AssertFailedException(
                    "{0}. Expected:<{1}> Actual<{2}>".FormatInvariant(errorMessage, typeof(TException).ToString(), ex.GetType().ToString()),
                    ex);
            }

            throw new AssertFailedException("{0}. Expected {1} exception but no exception is thrown".FormatInvariant(errorMessage, typeof(TException).ToString()));
        }

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
            if(expectedJson == null)
            {
                Assert.IsNull(actualJson, FormatExpectedActual("null", actualJson));
            }

            if(expectedJson.Length == 0)
            {
                Assert.AreEqual(actualJson.Length, 0, FormatExpectedActual("empty string", actualJson));
            }

            JObject expectedJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(expectedJson);
            JObject actualJObject = Newtonsoft.Json.JsonConvert.DeserializeObject<JObject>(actualJson);

            AreEqual(expectedJObject, actualJObject);
        }

        public static void AreEqual(JObject expectedJObject, JObject actualJObject)
        {
            if(expectedJObject == null)
            {
                Assert.IsNull(actualJObject, FormatExpectedActual("null", actualJObject.ToString()));
            }
            foreach (KeyValuePair<string, JToken> expectedPair in expectedJObject)
            {
                Object acturalValue = actualJObject.GetValue(expectedPair.Key);
                Assert.IsNotNull(acturalValue, FormatExpectedActual(expectedPair.Key + ":" + expectedPair.Value, "null"));
                if (expectedPair.Value.Type == JTokenType.Object)
                {
                    AreEqual((JObject)expectedPair.Value, (JObject)acturalValue);
                }
                else if (expectedPair.Value.Type == JTokenType.Array)
                {
                    AreEqual((JArray)expectedPair.Value, (JArray)acturalValue);
                }
                else
                {
                    Assert.AreEqual(expectedPair.Value, acturalValue, 
                        FormatExpectedActual(expectedPair.Key + ":" + expectedPair.Value, expectedPair.Key + ":" + acturalValue));
                }
            }
        }

        public static void AreEqual(JArray expectedJObject, JArray actualJObject)
        {
            Assert.AreEqual(expectedJObject.Count, actualJObject.Count);

            for (int index = 0; index < expectedJObject.Count; index++)
            {
                JToken expectedItem = expectedJObject[index];
                JToken actualItem = actualJObject[index];
                if (expectedItem.Type == JTokenType.Object)
                {
                    AreEqual((JObject)expectedItem, (JObject)actualItem);
                }
                else if (expectedItem.Type == JTokenType.Array)
                {
                    AreEqual((JArray)expectedItem, (JArray)actualItem);
                }
                else
                {
                    Assert.AreEqual(expectedItem, actualItem);
                }
            }
        }
    }
}
