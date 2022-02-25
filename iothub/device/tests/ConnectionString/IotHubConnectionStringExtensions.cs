// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Tests.ConnectionString
{
    internal class IotHubConnectionStringExtensions
    {
        internal static IotHubConnectionString Parse(string connectionString)
        {
            var builder = IotHubConnectionStringBuilder.Create(connectionString);
            return builder.ToIotHubConnectionString();
        }
    }
}
