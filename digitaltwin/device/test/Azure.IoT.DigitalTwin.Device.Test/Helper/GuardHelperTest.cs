// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Azure.Iot.DigitalTwin.Device.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;

namespace Azure.IoT.DigitalTwin.Device.Test.Bindings
{
    [TestClass]
    public class GuardHelperTest
    {
        [TestMethod]
        public void TestThrowIfNullWhenGivenNull()
        {
            string argumentName = "TestValue";
            try
            {
                GuardHelper.ThrowIfNull(null, argumentName);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual(argumentName, ((ArgumentNullException)ex).ParamName);
                return;
            }

            Assert.Fail("Expect exception thrown, but not happened.");
        }

        [TestMethod]
        public void TestThrowIfNullWhenGivenNotNull()
        {
            GuardHelper.ThrowIfNull(new object(), "TestValue");
        }

        [TestMethod]
        public void ThrowIfNullOrWhiteSpaceWhenGivenNull()
        {
            string argumentName = "TestValue";
            try
            {
                GuardHelper.ThrowIfNull(null, argumentName);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual(argumentName, ((ArgumentNullException)ex).ParamName);
                return;
            }

            Assert.Fail("Expect exception thrown, but not happened.");
        }

        [TestMethod]
        public void ThrowIfNullOrWhiteSpaceWhenGivenWhiteSpace()
        {
            string argumentName = "TestValue";
            try
            {
                GuardHelper.ThrowIfNullOrWhiteSpace(" ", argumentName);
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.AreEqual(argumentName, ((ArgumentNullException)ex).ParamName);
                return;
            }

            Assert.Fail("Expect exception thrown, but not happened.");
        }

        [TestMethod]
        public void ThrowIfNullOrWhiteSpaceWhenGivenNotNull()
        {
            GuardHelper.ThrowIfNullOrWhiteSpace("OneTwoThree", "TestValue");
        }

        [DataTestMethod]
        [DynamicData(nameof(GetInvalidInterfaceIdData), DynamicDataSourceType.Method)]
        public void ThrowIfInterfaceIdIsInvalid(string id)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(
                    id,
                    "instanceName",
                    Arg.Any<bool>(),
                    Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.AreEqual("id", ((ArgumentException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail($"Expected to throw exception for invalid interface id {id}.");
        }

        [DataTestMethod]
        [DynamicData(nameof(GetInvalidInterfaceInstanceNameData), DynamicDataSourceType.Method)]
        public void ThrowIfInterfaceInstanceNameIsInvalid(string instanceName)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(
                    "urn:id",
                    instanceName,
                    Arg.Any<bool>(),
                    Arg.Any<bool>());
            }
            catch (Exception ex)
            {
                Assert.AreEqual(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.AreEqual("instanceName", ((ArgumentException)ex).ParamName);
                Assert.IsTrue(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceInstanceNameErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.Fail($"Expected to throw exception for invalid interface name {instanceName}.");
        }

        private static IEnumerable<object[]> GetInvalidInterfaceIdData()
        {
            yield return new object[] { "urN:iwoerWE:RE309_4" };
            yield return new object[] { "ur:iwoerWER:RE309_4" };
            yield return new object[] { "urn:iwoerWER:RE309!4" };
            yield return new object[] { $"urn:{new string('A', 253)}" };
        }

        private static IEnumerable<object[]> GetInvalidInterfaceInstanceNameData()
        {
            yield return new object[] { "iwoerWE!" };
            yield return new object[] { "ur:iwoerWER_4" };
            yield return new object[] { $"{new string('A', 257)}" };
        }
    }
}
