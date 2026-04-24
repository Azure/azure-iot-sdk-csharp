// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// Device registration.
    /// </summary>
    internal partial class DeviceRegistration
    {
        /// <summary>
        /// Initializes a new instance of the DeviceRegistration class.
        /// </summary>
#pragma warning disable CA1812 //False positive on this issue. Complains about no one calling the constructor, but it is called in several places
        public DeviceRegistration()
        {
            CustomInit();
        }
#pragma warning restore CA1812

        /// <summary>
        /// Initializes a new instance of the DeviceRegistration class.
        /// </summary>
        public DeviceRegistration(JRaw payload = default)
        {
            Payload = payload;
            CustomInit();
        }

        /// <summary>
        /// An initialization method that performs custom operations like setting defaults
        /// </summary>
        partial void CustomInit();

        /// <summary>
        /// Gets or set the custom content payload.
        /// </summary>
        [JsonProperty(PropertyName = "payload")]
        public JRaw Payload { get; set; }
    }
}
