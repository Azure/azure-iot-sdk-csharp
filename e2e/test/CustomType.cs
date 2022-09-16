// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests
{
    /// <summary>
    /// This class is for direct methods in e2e tests and unit tests which takes custom type as payload type.
    /// </summary>
    public class CustomType
    {
        [JsonProperty("stringAttri")]
        internal string StringAttri;

        [JsonProperty("intAttri")]
        internal int IntAttri;

        [JsonProperty("boolAttri")]
        internal bool BoolAttri;

        [JsonProperty("nestedCustomType")]
        internal NestedCustomType NestedCustomType;

        public CustomType(string stringAttri, int intAttri, bool boolAttri, NestedCustomType nestedCustomType)
        {
            StringAttri = stringAttri;
            IntAttri = intAttri;
            BoolAttri = boolAttri;
            NestedCustomType = nestedCustomType;
        }

        public override string ToString()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>()
            {
                { "stringAttri", StringAttri },
                { "intAttri", IntAttri },
                { "boolAttri", BoolAttri },
                { "NestedCustomType", NestedCustomType },
            };

            return dict.ToString();
        }
    }
}
