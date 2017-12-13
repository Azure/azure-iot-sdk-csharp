// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class ParserUtilsTests
    {
        /* Codes_SRS_PARSER_UTILITY_21_001: [The IsValidUTF8 shall do nothing if the string is valid.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidUTF8_SucceedOnUTF8()
        {
            // arrange - act - assert
            ParserUtils.EnsureUTF8String("this-is-a-valid-UTF8");
        }

        /* Codes_SRS_PARSER_UTILITY_21_002: [The IsValidUTF8 shall throw ArgumentException if the provided string is null or empty.] */
        /* Codes_SRS_PARSER_UTILITY_21_003: [The IsValidUTF8 shall throw ArgumentException if the provided string contains at least one not UTF-8 character.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidUTF8_ThrowsOnInvalidUTF8()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String(null));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String(""));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String("  "));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureUTF8String("this is not a valid UTF8 \u1234"));
        }

        /* Codes_SRS_PARSER_UTILITY_21_004: [The IsValidBase64 shall do nothing if the string is valid.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidBase64_SucceedOnBase64()
        {
            // arrange - act - assert
            ParserUtils.EnsureBase64String("thisisavalidbase64==");
        }

        /* Codes_SRS_PARSER_UTILITY_21_005: [The IsValidBase64 shall throw ArgumentException if the provided string is null or empty.] */
        /* Codes_SRS_PARSER_UTILITY_21_006: [The IsValidBase64 shall throw ArgumentException if the provided string contains a non Base64 content.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidBase64_ThrowsOnInvalidBase64()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureBase64String(null));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureBase64String(""));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureBase64String("  "));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureBase64String("thisisnotavalidbase64="));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureBase64String("this is not a valid UTF8 \u1234"));
        }

        /* Codes_SRS_PARSER_UTILITY_21_007: [The IsValidRegistrationId shall do nothing if the string is a valid ID.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidRegistrationId_SucceedOnValidId()
        {
            // arrange - act - assert
            ParserUtils.EnsureRegistrationId("this-is-a-valid-registration-id");
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidRegistrationId_SucceedSizeLimitedTo128Chars()
        {
            // arrange - act - assert
            ParserUtils.EnsureRegistrationId(
                "12345678901234567890123456789012345678901234567890" +
                "12345678901234567890123456789012345678901234567890" +
                "1234567890123456789012345678");
        }

        /* Codes_SRS_PARSER_UTILITY_21_008: [The IsValidRegistrationId shall throw ArgumentException if the provided string is null or empty.] */
        /* Codes_SRS_PARSER_UTILITY_21_009: [The IsValidRegistrationId shall throw ArgumentException if the provided string contains more than 128 characters.] */
        /* Codes_SRS_PARSER_UTILITY_21_010: [The IsValidRegistrationId shall throw ArgumentException if the provided string contains an illegal character.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidRegistrationId_ThrowsOnInvalidId()
        {
            // arrange - act - assert
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId(null));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId(""));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId("  "));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId("invalid spaces"));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId("Invalid-Uppercase"));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId("invalidnonutf8\u1234"));
            TestAssert.Throws<ArgumentException>(() => ParserUtils.EnsureRegistrationId(
                "invalid-size-4567890123456789012345678901234567890" +
                "12345678901234567890123456789012345678901234567890" +
                "12345678901234567890123456789"));
        }

        /* Codes_SRS_PARSER_UTILITY_21_011: [The IsValidId shall do nothing if the string is a valid ID.] */
        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidId_SucceedOnValidId()
        {
            // arrange - act - assert
            ParserUtils.EnsureValidId("This-is:A.valid+Id%_#*?!(),=@;$\'");
        }

        [TestMethod]
        [TestCategory("DevService")]
        public void ParserUtils_IsValidId_SucceedSizeLimitedTo128Chars()
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
        [TestCategory("DevService")]
        public void ParserUtils_IsValidId_ThrowsOnInvalidId()
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
