// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Test.ConnectionString
{
    internal class IotHubConnectionStringExtensions
    {
        internal static IotHubConnectionInfo Parse(string connectionString)
        {
            var builder = new IotHubConnectionStringBuilder(connectionString);
            return builder.ToIotHubConnectionInfo();
        }
    }
}
