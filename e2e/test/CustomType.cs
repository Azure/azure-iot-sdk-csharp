// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class is for direct methods in e2e tests and unit tests which takes custom type as payload type.
    /// </summary>
    public class CustomType
    {
        public CustomType(string stringAttri, int intAttri, bool boolAttri, NestedCustomType nestedCustomType)
        {
            StringAttri = stringAttri;
            IntAttri = intAttri;
            BoolAttri = boolAttri;
            NestedCustomType = nestedCustomType;
        }

        [JsonPropertyName("stringAttri")]
        public string StringAttri { get; set; }

        [JsonPropertyName("intAttri")]
        public int IntAttri { get; set; }

        [JsonPropertyName("boolAttri")]
        public bool BoolAttri { get; set; }

        [JsonPropertyName("nestedCustomType")]
        public NestedCustomType NestedCustomType { get; set; }
    }
}
