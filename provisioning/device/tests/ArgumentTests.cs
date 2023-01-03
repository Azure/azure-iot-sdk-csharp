// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class ArgumentTests
    {
        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AssertNotNull_IfNull_Throws()
        {
            string argument = null;
            Argument.AssertNotNull<string>(argument, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssertNotNullOrWhiteSpace_IfEmptyString_Throws()
        {
            string argument = "";
            Argument.AssertNotNullOrWhiteSpace(argument, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssertNotNullOrWhiteSpace_IfWhitespaces_Throws()
        {
            string argument = " ";
            Argument.AssertNotNullOrWhiteSpace(argument, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AssertNotNullOrWhiteSpace_IfNull_Throws()
        {
            string argument = null;
            Argument.AssertNotNullOrWhiteSpace(argument, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void AssertNotNegativeValue_IfNegative_Throws()
        {
            var argument = TimeSpan.FromHours(-27.75);
            Argument.AssertNotNegativeValue<TimeSpan>(argument, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AssertNotNullOrEmpty_IfEmpty_Throws()
        {
            var argument = new List<string>();
            Argument.AssertNotNullOrEmpty<string>(argument, "");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AssertNotNullOrEmpty_IfNull_Throws()
        {
            List<string> argument = null;
            Argument.AssertNotNullOrEmpty<string>(argument, "");
        }
    }
}
