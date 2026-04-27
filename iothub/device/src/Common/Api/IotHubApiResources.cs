// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client.Common.Api;

namespace Microsoft.Azure.Devices.Client
{
    internal sealed class IotHubApiResources : ApiResources
    {
        internal static string GetString(string value, params object[] args)
        {
            return string.Format(Culture, value, args);
        }
    }
}
