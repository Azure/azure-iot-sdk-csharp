// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Options that allow configuration of the <see cref="ProvisioningServiceClient"/> instance during initialization.
    /// </summary>
    public class ProvisioningServiceClientOptions
    {
        /// <summary>
        /// The Device Provisioning Service data-plane (service) API version the client will use.
        /// </summary>
        /// <remarks>
        /// Defaults to <see cref="ServiceVersion.V2019_03_31"/> so existing callers see no wire change on
        /// upgrade. <see cref="ServiceVersion.V2026_11_01"/> and <see cref="ServiceVersion.V2026_11_02_Preview"/>
        /// are explicit opt-in versions; select one to send 2026 requests. The preview version additionally
        /// enables preview-only fields (for example <c>deviceTypeRefs</c>).
        /// Must be a defined <see cref="ServiceVersion"/> value; the default enum value (0) is not a valid
        /// version and will cause requests to fail.
        /// </remarks>
        public ServiceVersion Version { get; set; } = ServiceVersion.V2019_03_31;
    }
}
