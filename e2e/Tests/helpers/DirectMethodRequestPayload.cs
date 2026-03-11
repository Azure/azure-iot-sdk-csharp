// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json.Serialization;

namespace Microsoft.Azure.Devices.E2ETests.Helpers
{
    internal class DirectMethodRequestPayload
    {
        [JsonPropertyName("desiredState")]
        public string DesiredState { get; set; }
    }
}
