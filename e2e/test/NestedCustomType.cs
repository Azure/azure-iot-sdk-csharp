// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class is for direct methods in e2e tests and unit tests which takes nested custom type as payload type.
    /// </summary>
    public class NestedCustomType
    {
        [JsonPropertyName("stringAttri")]
        private string StringAttri;

        [JsonPropertyName("intAttri")]
        private int IntAttri;

        public NestedCustomType(string stringAttri, int intAttri)
        {
            StringAttri = stringAttri;
            IntAttri = intAttri;
        }

        public override string ToString()
        {
            var dict = new Dictionary<string, object>()
            {
                { "stringAttri", StringAttri },
                { "intAttri", IntAttri },
            };

            return dict.ToString();
        }
    }
}
