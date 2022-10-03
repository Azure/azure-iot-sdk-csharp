// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// This is the abstract class that unifies all possible types of attestation that Device Provisioning Service supports.
    /// </summary>
    /// <remarks>
    /// For now, the provisioning service supports <see cref="X509Attestation"/> and <see cref="SymmetricKeyAttestation"/>.
    /// </remarks>
    public abstract class Attestation
    {
        // Abstract class fully implemented by the child.
    }
}
