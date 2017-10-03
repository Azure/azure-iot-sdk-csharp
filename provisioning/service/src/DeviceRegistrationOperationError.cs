// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    public sealed class DeviceRegistrationOperationError
    {
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }

        [JsonProperty(PropertyName = "errorCode")]
        public string ErrorCode { get; set; }

        [JsonProperty(PropertyName = "errorStatus")]
        public string ErrorStatus { get; set; }
    }
}
