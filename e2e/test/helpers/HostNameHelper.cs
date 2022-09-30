// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal static class HostNameHelper
    {
        // Look for "HostName=", and then grab all the characters until just before the next semi-colon.
        private static readonly Regex s_hostNameRegex = new("(?<=HostName=).*?(?=;)", RegexOptions.Compiled);

        /// <summary>
        /// Extracts the IoT hub host name from the specified connection string
        /// </summary>
        public static string GetHostName(string iotHubConnectionString)
        {
            return s_hostNameRegex.Match(iotHubConnectionString).Value;
        }
    }
}
