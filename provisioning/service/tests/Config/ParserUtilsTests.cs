﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    [TestCategory("Unit")]
    public class ParserUtilsTests
    {
        /* Codes_SRS_PARSER_UTILITY_21_001: [The IsValidUTF8 shall do nothing if the string is valid.] */
        [TestMethod]
        public void ParserUtilsIsValidUTF8SucceedOnUTF8()
        {
            // arrange - act - assert
            ParserUtils.EnsureUTF8String("this-is-a-valid-UTF8");
        }

        /* Codes_SRS_PARSER_UTILITY_21_002: [The IsValidUTF8 shall throw ArgumentException if the provided string is null or empty.] */
        /* Codes_SRS_PARSER_UTILITY_21_003: [The IsValidUTF8 shall throw ArgumentException if the provided string contains at least one not UTF-8 character.] */
        [TestMethod]
        public void ParserUtilsIsValidUTF8ThrowsOnInvalidUTF8()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String(null));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String(""));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String("  "));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String("this is not a valid UTF8 \u1234"));
        }

        /* Codes_SRS_PARSER_UTILITY_21_011: [The IsValidId shall do nothing if the string is a valid ID.] */
        [TestMethod]
        public void ParserUtilsIsValidIdSucceedOnValidId()
        {
            // arrange - act - assert
            ParserUtils.EnsureValidId("This-is:A.valid+Id%_#*?!(),=@;$\'");
        }

        [TestMethod]
        public void ParserUtilsIsValidIdSucceedSizeLimitedTo128Chars()
        {
            // arrange - act - assert
            ParserUtils.EnsureValidId(
                "12345678901234567890123456789012345678901234567890" +
                "12345678901234567890123456789012345678901234567890" +
                "1234567890123456789012345678");
        }

        /* Codes_SRS_PARSER_UTILITY_21_012: [The IsValidId shall throw ArgumentException if the provided string is null or empty.] */
        /* Codes_SRS_PARSER_UTILITY_21_013: [The IsValidId shall throw ArgumentException if the provided string contains more than 128 characters.] */
        /* Codes_SRS_PARSER_UTILITY_21_014: [The IsValidId shall throw ArgumentException if the provided string contains an illegal character.] */
        [TestMethod]
        public void ParserUtilsIsValidIdThrowsOnInvalidId()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureValidId(null));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureValidId(""));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureValidId("  "));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureValidId("Valid Id cannot have spaces"));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureValidId("ValididShallbeUTF8\u1234"));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureValidId(
                "12345678901234567890123456789012345678901234567890" +
                "12345678901234567890123456789012345678901234567890" +
                "12345678901234567890123456789"));
        }
    }
}
