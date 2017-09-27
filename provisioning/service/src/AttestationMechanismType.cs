// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;

    /// <summary>
    /// Type of attestation mechanism.
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum AttestationMechanismType
    {
        None = 0,
        Tpm = 1,
        X509 = 2
    }
}