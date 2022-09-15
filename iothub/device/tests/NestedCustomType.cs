// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    internal class NestedCustomType
    {
        [JsonProperty]
        private string stringAttri;

        [JsonProperty]
        private int intAttri;

        public NestedCustomType(string stringAttri, int intAttri)
        {
            this.stringAttri = stringAttri;
            this.intAttri = intAttri;
        }

        public override string ToString()
        {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("stringAttri", stringAttri);
            dict.Add("intAttri", intAttri);

            return dict.ToString();
        }
    }
}
