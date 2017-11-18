// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    using System;
    using System.Globalization;
    using System.Threading.Tasks;

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

        public static TException Throws<TException>(Func<Task> action, string errorMessage = null) where TException : Exception
        {
            return Throws<TException>(() => action().Wait(), errorMessage);
        }

        public static async Task<TException> ThrowsAsync<TException>(Func<Task> action, string errorMessage = null) where TException : Exception
        {
            errorMessage = errorMessage ?? "Failed";
            try
            {
                await action();
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

        public static void assertJson(string expectedJson, string actualJson)
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

            assertJObject(expectedJObject, actualJObject);
        }

        public static void assertJObject(JObject expectedJObject, JObject actualJObject)
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
                    Assert.IsTrue(acturalValue is JTokenType, 
                        FormatExpectedActual(expectedPair.Value.ToString(), acturalValue.ToString()));
                    assertJObject((JObject)expectedPair.Value, (JObject)acturalValue);
                }
                else
                {
                    Assert.AreEqual(expectedPair.Value, acturalValue, 
                        FormatExpectedActual(expectedPair.Key + ":" + expectedPair.Value, expectedPair.Key + ":" + acturalValue));
                }
            }
        }
    }
}
