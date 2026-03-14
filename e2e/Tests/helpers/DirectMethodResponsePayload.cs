// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class DirectMethodResponsePayload
    {
        [JsonProperty("currentState")]
        public string CurrentState { get; set; }
    }
}
