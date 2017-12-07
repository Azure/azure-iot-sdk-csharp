// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.Devices.Common;

    /// <summary>
    /// X509 client certificate thumbprints of the device
    /// </summary>
    public static class X509ThumbprintExtensions
    {
        static readonly Regex ThumbprintRegex = new Regex(@"^([A-Fa-f0-9]{2}){20}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        /// <summary>
        /// Checks if contents are valid
        /// </summary>
        /// <param name="throwArgumentException"></param>
        /// <returns>bool</returns>
        public static bool IsValid(this X509Thumbprint x509Thumbprint, bool throwArgumentException)
        {
            if (!x509Thumbprint.IsEmpty())
            {
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

        public static bool IsValidThumbprint(string thumbprint)
        {
            return thumbprint != null && ThumbprintRegex.IsMatch(thumbprint);
        }
    }
}