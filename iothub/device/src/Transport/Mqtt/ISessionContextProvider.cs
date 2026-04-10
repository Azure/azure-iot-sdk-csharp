// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal interface ISessionContextProvider
    {
        IDictionary<string, string> Properties { get; set; }
    }
}
