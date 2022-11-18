﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices
{
    internal class QuerySpecification
    {
        [JsonPropertyName("query", Required = Required.Always)]
        internal string Sql { get; set; }
    }
}
