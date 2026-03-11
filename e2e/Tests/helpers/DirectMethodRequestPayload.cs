// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class DirectMethodRequestPayload
    {
        [JsonProperty("desiredState")]
        public string DesiredState { get; set; }
    }
}
