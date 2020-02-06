// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Azure.Devices.DigitalTwin.Client.Helper;
using NSubstitute;
using Xunit;

namespace Microsoft.Azure.Devices.DigitalTwin.Client.Test.Helper
{
    [Trait("TestCategory", "DigitalTwin")]
    [Trait("TestCategory", "Unit")]
    public class GuardHelperTest
    {
        [Fact]
        public void TestThrowIfNullWhenGivenNull()
        {
            string argumentName = "TestValue";
            try
            {
                GuardHelper.ThrowIfNull(null, argumentName);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal(argumentName, ((ArgumentNullException)ex).ParamName);
                return;
            }

            Assert.True(false, "Expect exception thrown, but not happened.");
        }

        [Fact]
        public void TestThrowIfNullWhenGivenNotNull()
        {
            GuardHelper.ThrowIfNull(new object(), "TestValue");
        }

        [Fact]
        public void ThrowIfNullOrWhiteSpaceWhenGivenNull()
        {
            string argumentName = "TestValue";
            try
            {
                GuardHelper.ThrowIfNull(null, argumentName);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal(argumentName, ((ArgumentNullException)ex).ParamName);
                return;
            }

            Assert.True(false, "Expect exception thrown, but not happened.");
        }

        [Fact]
        public void ThrowIfNullOrWhiteSpaceWhenGivenWhiteSpace()
        {
            string argumentName = "TestValue";
            try
            {
                GuardHelper.ThrowIfNullOrWhiteSpace(" ", argumentName);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentNullException).FullName, ex.GetType().FullName);
                Assert.Equal(argumentName, ((ArgumentNullException)ex).ParamName);
                return;
            }

            Assert.True(false, "Expect exception thrown, but not happened.");
        }

        [Fact]
        public void ThrowIfNullOrWhiteSpaceWhenGivenNotNull()
        {
            GuardHelper.ThrowIfNullOrWhiteSpace("OneTwoThree", "TestValue");
        }

        [Theory]
        [MemberData(nameof(GetInvalidInterfaceIdData))]
        public void ThrowIfInterfaceIdIsInvalid(string id)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient(id, "instanceName");
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.Equal("id", ((ArgumentException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceIdErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false, $"Expected to throw exception for invalid interface id {id}.");
        }

        [Theory]
        [MemberData(nameof(GetInvalidInterfaceInstanceNameData))]
        public void ThrowIfInterfaceInstanceNameIsInvalid(string instanceName)
        {
            try
            {
                var client = new DigitalTwinInterfaceTestClient("urn:id", instanceName);
            }
            catch (Exception ex)
            {
                Assert.Equal(typeof(ArgumentException).FullName, ex.GetType().FullName);
                Assert.Equal("instanceName", ((ArgumentException)ex).ParamName);
                Assert.True(ex.Message?.StartsWith(DigitalTwinConstants.InvalidInterfaceInstanceNameErrorMessage, StringComparison.Ordinal) ?? false);
                return;
            }

            Assert.True(false, $"Expected to throw exception for invalid interface name {instanceName}.");
        }

        public static IEnumerable<object[]> GetInvalidInterfaceIdData =>
            new List<object[]>
            {
                new object[] { "urN:iwoerWE:RE309_4" },
                new object[] { "ur:iwoerWER:RE309_4" },
                new object[] { "urn:iwoerWER:RE309!4" },
                new object[] { $"urn:{new string('A', 253)}" },
            };

        public static IEnumerable<object[]> GetInvalidInterfaceInstanceNameData =>
            new List<object[]>
            {
                new object[] { "iwoerWE!" },
                new object[] { "ur:iwoerWER_4" },
                new object[] { $"{new string('A', 257)}" },
            };
    }
}
