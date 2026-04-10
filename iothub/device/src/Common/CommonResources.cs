// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class CommonResources
        : Common.Resources
    {
        internal static string GetString(string value, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    if (args[i] is string text
                        && text != null
                        && text.Length > 1024)
                    {
                        args[i] = text.Substring(0, 1021) + "...";
                    }
                }

                return string.Format(Culture, value, args);
            }

            return value;
        }

        internal static string GetNewStringGuid(string postfix = "")
        {
            return Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture) + postfix;
        }
    }
}
