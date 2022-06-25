// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    ///     A Json query request
    /// </summary>
    internal class QuerySpecification
    {
        [JsonProperty(PropertyName = "query", Required = Required.Always)]
        public string Sql { get; set; }
    }
}