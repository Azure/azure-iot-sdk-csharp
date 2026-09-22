// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The Device Provisioning Service data-plane (service) API version used by the
    /// <see cref="ProvisioningServiceClient"/>.
    /// </summary>
    /// <remarks>
    /// The version determines the <c>api-version</c> query string sent on every request and which
    /// version-specific payload fields are serialized on outgoing requests. Deserialization of
    /// responses is always tolerant of all known fields regardless of the selected version.
    /// </remarks>
    public enum ServiceVersion
    {
        /// <summary>
        /// API version <c>2019-03-31</c>. This is the default used when no explicit version is selected.
        /// </summary>
        V2019_03_31 = 1,

        /// <summary>
        /// API version <c>2026-11-01</c> (generally available contract). An explicit opt-in version; it is
        /// <b>not</b> the default. Select it to send <c>2026-11-01</c> requests, which support the optional
        /// <c>namespaceName</c>, <c>certificateAuthorityName</c>, and <c>certificatePolicyName</c> fields.
        /// </summary>
        V2026_11_01 = 2,

        /// <summary>
        /// API version <c>2026-11-02-preview</c>. An explicit opt-in version; it is <b>not</b> the default.
        /// In addition to the <c>2026-11-01</c> fields, it enables preview-only fields (for example
        /// <c>deviceTypeRefs</c>), which are only serialized when this version is selected.
        /// </summary>
        V2026_11_02_Preview = 3,
    }
}
