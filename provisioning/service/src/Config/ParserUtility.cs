// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Set of static functions to help the serialize, deserialize, and validate JSON.
    /// </summary>
    internal static class ParserUtils
    {

        /// <summary>
        /// Helper to validate if the provided string is not null, empty, and all characters are UTF-8.
        /// </summary>
        /// <param name="input">the <code>string</code> to be validated</param>
        /// <exception cref="ArgumentException">if the provided <code>string</code> do not fits the criteria</exception>
        public static void EnsureUTF8String(string input)
        {
            if(string.IsNullOrWhiteSpace(input))
            {
                /* Codes_SRS_PARSER_UTILITY_21_002: [The IsValidUTF8 shall throw ArgumentException if the provided string 
                                                    is null or empty.] */
                throw new ArgumentException("parameter cannot be null or whiteSpace");
            }

            /* Codes_SRS_PARSER_UTILITY_21_003: [The IsValidUTF8 shall throw ArgumentException if the provided string contains 
                                                    at least one not UTF-8 character.] */
            try
            {
                int lenghUtf8 = Encoding.UTF8.GetByteCount(input);
                if(lenghUtf8 != input.Length)
                {
                    throw new ArgumentException("parameter contains non UTF8 character");
                }
            }
            catch(EncoderFallbackException e)
            {
                throw new ArgumentException("parameter contains non UTF8 character", e);
            }

            /* Codes_SRS_PARSER_UTILITY_21_001: [The IsValidUTF8 shall do nothing if the string is valid.] */
        }

        /// <summary>
        /// Helper to validate if the provided string is not null, empty, and Base64.
        /// </summary>
        /// <param name="input">the <code>string</code> to be validated</param>
        /// <exception cref="ArgumentException">if the provided <code>string</code> do not fits the criteria</exception>
        public static void EnsureBase64String(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                /* Codes_SRS_PARSER_UTILITY_21_005: [The IsValidBase64 shall throw ArgumentException if the provided string 
                                                    is null or empty.] */
                throw new ArgumentException("parameter cannot be null or whiteSpace");
            }

            /* Codes_SRS_PARSER_UTILITY_21_006: [The IsValidBase64 shall throw ArgumentException if the provided string 
                                                    contains a non Base64 content.] */
            try
            {
                Convert.FromBase64String(input);
            }
            catch (FormatException e)
            {
                throw new ArgumentException("parameter is not a valid Base64 string", e);
            }

            /* Codes_SRS_PARSER_UTILITY_21_004: [The IsValidBase64 shall do nothing if the string is valid.] */
        }

        /// <summary>
        /// Helper to validate RegistrationId.
        /// </summary>
        /// <remarks>
        /// A valid registration Id shall be alphanumeric, lowercase, and may contain hyphens. Max characters 128.
        /// </remarks>
        /// <param name="input">the <code>string</code> to be validated</param>
        /// <exception cref="ArgumentException">if the provided <code>string</code> do not fits the criteria</exception>
        public static void EnsureRegistrationId(string input)
        {
            /* Codes_SRS_PARSER_UTILITY_21_008: [The IsValidRegistrationId shall throw ArgumentException if the provided 
                                                string is null or empty.] */
            try
            {
                EnsureUTF8String(input);
            }
            catch(ArgumentException)
            {
                throw new ArgumentException("The provided ID contains non UTF-8 character");
            }

            /* Codes_SRS_PARSER_UTILITY_21_009: [The IsValidRegistrationId shall throw ArgumentException if the provided string 
                                                contains more than 128 characters.] */
            if (input.Length > 128)
            {
                throw new ArgumentException("The provided ID is bigger than 128 characters");
            }

            /* Codes_SRS_PARSER_UTILITY_21_010: [The IsValidRegistrationId shall throw ArgumentException if the provided string 
                                                contains an illegal character.] */
            char[] chars = input.ToCharArray();
            foreach (char c in chars)
            {
                if (!(((c >= 'a') && (c <= 'z')) || ((c >= '0') && (c <= '9')) || (c == '-')))
                {
                    throw new ArgumentException("The provided ID contains non valid character");
                }
            }

            /* Codes_SRS_PARSER_UTILITY_21_007: [The IsValidRegistrationId shall do nothing if the string is a valid ID.] */
        }

        /// <summary>
        /// Helper to validate if the provided string is not null, empty, or invalid Id.
        /// </summary>
        /// <param name="input">the <code>string</code> to be validated</param>
        /// <exception cref="ArgumentException">if the provided <code>string</code> do not fits the criteria</exception>
        public static void EnsureValidId(string input)
        {
            /* Codes_SRS_PARSER_UTILITY_21_012: [The IsValidId shall throw ArgumentException if the provided string 
                                                is null or empty.] */
            try
            {
                EnsureUTF8String(input);
            }
            catch (ArgumentException)
            {
                throw new ArgumentException("The provided ID contains non UTF-8 character");
            }

            /* Codes_SRS_PARSER_UTILITY_21_013: [The IsValidId shall throw ArgumentException if the provided string 
                                                contains more than 128 characters.] */
            if (input.Length > 128)
            {
                throw new ArgumentException("The provided ID is bigger than 128 characters");
            }

            /* Codes_SRS_PARSER_UTILITY_21_014: [The IsValidId shall throw ArgumentException if the provided string 
                                                contains an illegal character.] */
            char[] chars = input.ToCharArray();
            foreach (char c in chars)
            {
                if (!(((c >= 'A') && (c <= 'Z')) || ((c >= 'a') && (c <= 'z')) || ((c >= '0') && (c <= '9')) ||
                        (c == '-') || (c == ':') || (c == '.') || (c == '+') || (c == '%') || (c == '_') || 
                        (c == '#') || (c == '*') || (c == '?') || (c == '!') || (c == '(') || (c == ')') || 
                        (c == ',') || (c == '=') || (c == '@') || (c == ';') || (c == '$') || (c == '\'')))
                {
                    throw new ArgumentException("The provided ID contains non valid character");
                }
            }

            /* Codes_SRS_PARSER_UTILITY_21_011: [The IsValidId shall do nothing if the string is a valid ID.] */
        }
    }
}
