// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Models
{
    /// <summary>
    /// The ArcEnabledDevice onboarding response.
    /// </summary>
    internal class ArcEnabledDeviceResponse : ResponseMetadata
    {
        /// <summary> Initializes a new instance of ArcEnabledDeviceResponse. </summary>
        internal ArcEnabledDeviceResponse()
        {
            Kind = "ArcEnabledDeviceResponse";
        }
    }
}