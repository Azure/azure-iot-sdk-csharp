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
        private static readonly string s_fakeRegistrationId = "registrationId";
        private static readonly string s_fakePrimaryKey = "dGVzdFN0cmluZbB=";
        private static readonly string s_fakeSecondaryKey = "wGVzdFN9CmluZaA=";

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_Works()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, s_fakePrimaryKey, s_fakeSecondaryKey);

            // assert
            act.Should().NotThrow();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_RegistrationId_EmptyString_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey("", s_fakePrimaryKey, s_fakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_RegistrationId_Null_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey(null, s_fakePrimaryKey, s_fakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_PrimaryKey_EmptyString_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, "", s_fakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_PrimaryKey_Null_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, null, s_fakeSecondaryKey);

            // assert
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_SecondaryKey_EmptyString_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, s_fakePrimaryKey, "");

            // assert
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AuthenticationProviderSymmetricKey_SecondaryKey_Null_Throws()
        {
            // arrange - act
            Func<AuthenticationProviderSymmetricKey> act = () => new AuthenticationProviderSymmetricKey(s_fakeRegistrationId, s_fakePrimaryKey, null);

            // assert
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
