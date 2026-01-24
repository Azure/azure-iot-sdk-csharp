// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// Additional parameters to be passed over registartion instance
    /// </summary>
    /// <returns>The registration result.</returns>
    public class ProvisioningRegistrationAdditionalData
    {
        /// <summary>
        /// Additional (optional) Json Data to be sent to the service 
        /// </summary>
        public string JsonData { get; set; }
    }
}
