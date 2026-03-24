// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    public class NestedObject
    {
        [JsonPropertyName("someDouble")]
        public double SomeDouble { get; set; }

        [JsonPropertyName("arrayValues")]
        public IList<object> ArrayValues { get; set; } = new List<object>();
    }
}
