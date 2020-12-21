// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// X509 client certificate thumbprints of the device
    /// </summary>
    public static class X509ThumbprintExtensions
    {
        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex s_sha1ThumbprintRegex = new Regex(@"^([a-f0-9]{2}){20}$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sha256ThumbprintRegex = new Regex(@"^([a-f0-9]{2}){32}$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);

        /// <summary>
        /// Checks if contents are valid
        /// </summary>
        /// <param name="x509Thumbprint">The X509 thumbprint to check.</param>
        /// <param name="throwArgumentException">A flag to indicat whether an <see cref="ArgumentException"/> should be thrown.</param>
        /// <returns>bool</returns>
        public static bool IsValid(this X509Thumbprint x509Thumbprint, bool throwArgumentException)
        {
            if (!x509Thumbprint.IsEmpty())
            {
                int primaryThumbprintLength = x509Thumbprint.PrimaryThumbprint?.Length ?? 0;
                int secondaryThumbprintLength = x509Thumbprint.SecondaryThumbprint?.Length ?? 0;

                if (primaryThumbprintLength != secondaryThumbprintLength)
                {
                    if (primaryThumbprintLength != 0 && secondaryThumbprintLength != 0)
                    {
                        throw new ArgumentException(ApiResources.ThumbprintSizesMustMatch);
                    }
                }
                
                bool primaryThumbprintIsValid = X509ThumbprintExtensions.IsValidThumbprint(x509Thumbprint.PrimaryThumbprint);
                bool secondaryThumbprintIsValid = X509ThumbprintExtensions.IsValidThumbprint(x509Thumbprint.SecondaryThumbprint);

                if (primaryThumbprintIsValid)
                {
                    if (secondaryThumbprintIsValid || string.IsNullOrWhiteSpace(x509Thumbprint.SecondaryThumbprint))
                    {
                        return true;
                    }
                    else
                    {
                        if (throwArgumentException)
                        {
                            throw new ArgumentException(ApiResources.StringIsNotThumbprint.FormatInvariant(x509Thumbprint.SecondaryThumbprint), "Secondary Thumbprint");
                        }

                        return false;
                    }
                }
                else if (secondaryThumbprintIsValid)
                {
                    if (string.IsNullOrWhiteSpace(x509Thumbprint.PrimaryThumbprint))
                    {
                        return true;
                    }
                    else
                    {
                        if (throwArgumentException)
                        {
                            throw new ArgumentException(ApiResources.StringIsNotThumbprint.FormatInvariant(x509Thumbprint.PrimaryThumbprint), "PrimaryThumbprint");
                        }

                        return false;
                    }
                }

                if (throwArgumentException)
                {
                    if (string.IsNullOrEmpty(x509Thumbprint.SecondaryThumbprint))
                    {
                        throw new ArgumentException(ApiResources.StringIsNotThumbprint.FormatInvariant(x509Thumbprint.PrimaryThumbprint), "Primary Thumbprint");
                    }
                    else if (string.IsNullOrEmpty(x509Thumbprint.PrimaryThumbprint))
                    {
                        throw new ArgumentException(ApiResources.StringIsNotThumbprint.FormatInvariant(x509Thumbprint.SecondaryThumbprint), "Secondary Thumbprint");
                    }
                    else
                    {
                        throw new ArgumentException(ApiResources.StringIsNotThumbprint.FormatInvariant(x509Thumbprint.PrimaryThumbprint), "Primary Thumbprint");
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if the thumbprints are populated
        /// </summary>
        /// <returns>bool</returns>
        public static bool IsEmpty(this X509Thumbprint x509Thumbprint)
        {
            return string.IsNullOrWhiteSpace(x509Thumbprint.PrimaryThumbprint) && string.IsNullOrWhiteSpace(x509Thumbprint.SecondaryThumbprint);
        }

        /// <summary>
        /// Checks if the thumbprint is valid.
        /// </summary>
        /// <param name="thumbprint">The thumbprint to check.</param>
        /// <returns>True if the thumbprint is valid.</returns>
        public static bool IsValidThumbprint(string thumbprint)
        {
            return thumbprint != null &&
                ( s_sha1ThumbprintRegex.IsMatch(thumbprint) || (s_sha256ThumbprintRegex.IsMatch(thumbprint)));
        }
    }
}
