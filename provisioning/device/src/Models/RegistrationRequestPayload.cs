// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Optional data to be included in the registration request.
    /// </summary>
    public class RegistrationRequestPayload
    {
        /// <summary>
        /// Additional (optional) Json Data to be sent to the service 
        /// </summary>
        public string JsonData { get; set; }
    }
}
