﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Device registration over HTTP.
    /// </summary>
    internal class DeviceRegistrationHttp : DeviceRegistration
    {
        /// <summary>
        /// Creates an instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistrationHttp(JRaw payload = default, string registrationId = default)
            : base(payload)
        {
            RegistrationId = registrationId;
        }

        /// <summary>
        /// The device registration Id.
        /// </summary>
        [JsonProperty(PropertyName = "registrationId")]
        public string RegistrationId { get; set; }
    }
}
