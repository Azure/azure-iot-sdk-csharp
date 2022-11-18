// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class is for direct methods in e2e tests and unit tests which takes custom type as payload type.
    /// </summary>
    public class CustomType
    {
        [JsonPropertyName("stringAttri")]
        public string StringAttri;

        [JsonPropertyName("intAttri")]
        public int IntAttri;

        [JsonPropertyName("boolAttri")]
        public bool BoolAttri;

        [JsonPropertyName("nestedCustomType")]
        public NestedCustomType NestedCustomType;

        public CustomType(string stringAttri, int intAttri, bool boolAttri, NestedCustomType nestedCustomType)
        {
            StringAttri = stringAttri;
            IntAttri = intAttri;
            BoolAttri = boolAttri;
            NestedCustomType = nestedCustomType;
        }
    }
}
