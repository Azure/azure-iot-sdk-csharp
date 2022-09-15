// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    internal class CustomType
    {
        [JsonProperty]
        internal string stringAttri;

        [JsonProperty]
        internal int intAttri;

        [JsonProperty]
        internal bool boolAttri;

        [JsonProperty]
        internal NestedCustomType NestedCustomType;

        public CustomType(string stringAttri, int intAttri, bool boolAttri, NestedCustomType nestedCustomType)
        {
            this.stringAttri = stringAttri;
            this.intAttri = intAttri;
            this.boolAttri = boolAttri;
            this.NestedCustomType = nestedCustomType;
        }

        public override string ToString()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("stringAttri", stringAttri);
            dict.Add("intAttri", intAttri);
            dict.Add("boolAttri", boolAttri);
            dict.Add("NestedCustomType", NestedCustomType);

            return dict.ToString();
        }
    }
}
