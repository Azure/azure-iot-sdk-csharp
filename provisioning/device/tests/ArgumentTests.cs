// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ArgumentTests
    {
        [TestMethod]
        public void AssertNotNull_IfNull_Throws()
        {
            Action act = () => Argument.AssertNotNull<string>(null, "parameterName");
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AssertNotNullOrWhiteSpace_IfEmptyString_Throws()
        {
            Action act = () => Argument.AssertNotNullOrWhiteSpace("", "parameterName");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AssertNotNullOrWhiteSpace_IfWhitespaces_Throws()
        {
            Action act = () => Argument.AssertNotNullOrWhiteSpace(" ", "parameterName");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AssertNotNullOrWhiteSpace_IfNull_Throws()
        {
            Action act = () => Argument.AssertNotNullOrWhiteSpace(null, "parameterName");
            act.Should().Throw<ArgumentNullException>();
        }

        [TestMethod]
        public void AssertNotNegativeValue_IfNegative_Throws()
        {
            Action act = () => Argument.AssertNotNegativeValue<TimeSpan>(TimeSpan.FromHours(-27.75), "parameterName");
            act.Should().Throw<ArgumentOutOfRangeException>();
        }

        [TestMethod]
        public void AssertNotNullOrEmpty_IfEmpty_Throws()
        {
            Action act = () => Argument.AssertNotNullOrEmpty<string>(new List<string>(), "parameterName");
            act.Should().Throw<ArgumentException>();
        }

        [TestMethod]
        public void AssertNotNullOrEmpty_IfNull_Throws()
        {
            Action act = () => Argument.AssertNotNullOrEmpty<string>(null, "parameterName");
            act.Should().Throw<ArgumentNullException>();
        }
    }
}
