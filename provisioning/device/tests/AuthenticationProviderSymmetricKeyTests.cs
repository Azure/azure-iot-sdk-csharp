// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class AuthenticationProviderSymmetricKeyTests
    {
        private const string FakeRegistrationId = "registrationId";
        private const string FakePrimaryKey = "dGVzdFN0cmluZbB=";
        private const string FakeSecondaryKey = "wGVzdFN9CmluZaA=";

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_Works()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey(FakeRegistrationId, FakePrimaryKey, FakeSecondaryKey);

            // assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_RegistrationId_EmptyString_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey("", FakePrimaryKey, FakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentException>().WithParameterName("registrationId");
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_RegistrationId_Null_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey(null, FakePrimaryKey, FakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("registrationId");
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_PrimaryKey_EmptyString_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey(FakeRegistrationId, "", FakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentException>().WithParameterName("primaryKey");
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_PrimaryKey_Null_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey(FakeRegistrationId, null, FakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("primaryKey");
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_SecondaryKey_EmptyString_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey(FakeRegistrationId, FakePrimaryKey, "");

            // assert
            act.Should().Throw<ArgumentException>().WithParameterName("secondaryKey");
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_SecondaryKey_Null_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => _ = new AuthenticationProviderSymmetricKey(FakeRegistrationId, FakePrimaryKey, null);

            // assert
            act.Should().Throw<ArgumentNullException>().WithParameterName("secondaryKey");
        }
    }
}
