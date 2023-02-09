// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client.Utilities
{
    internal sealed class StringValidationHelper
    {
        private const char Base64Padding = '=';
        private const string StringIsNotBase64 = "Value '{0}' for parameter '{1}' is not Base64.";

        private static readonly HashSet<char> s_base64Table = new()
        {
            'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
            'P','Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d',
            'e','f','g','h','i','j','k','l','m','n','o','p','q','r','s',
            't','u','v','w','x','y','z','0','1','2','3','4','5','6','7',
            '8','9','+','/'
        };

        internal static void EnsureBase64String(string value, string paramName)
        {
            if (!IsBase64StringValid(value))
            {
                throw new FormatException(string.Format(CultureInfo.InvariantCulture, StringIsNotBase64, value, paramName));
            }
        }

        internal static bool IsBase64StringValid(string value)
        {
            return value != null && IsBase64String(value);
        }

        internal static bool IsBase64String(string value)
        {
            value = value.Replace("\r", string.Empty)
                .Replace("\n", string.Empty);
            if (value.Length == 0 || value.Length % 4 != 0)
            {
                return false;
            }

            int lengthNoPadding = value.Length;
            value = value.TrimEnd(Base64Padding);
            int lengthPadding = value.Length;

            if (lengthNoPadding - lengthPadding > 2)
            {
                return false;
            }

            foreach (char c in value)
            {
                if (!s_base64Table.Contains(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
