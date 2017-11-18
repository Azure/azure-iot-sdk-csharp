// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Provisioning.Service.Test
{
    [TestClass]
    public class AttestationMechanismTests
    {
        /* SRS_ATTESTATION_MECHANISM_21_001: [The constructor shall throw ArgumentException if the provided attestation is null.] */
        /* SRS_ATTESTATION_MECHANISM_21_002: [If the provided attestation is instance of TpmAttestation, the constructor shall store the provided tpm keys.] */
        /* SRS_ATTESTATION_MECHANISM_21_003: [If the provided attestation is instance of TpmAttestation, the constructor shall set the attestation type as TPM.] */
        /* SRS_ATTESTATION_MECHANISM_21_004: [If the provided attestation is instance of TpmAttestation, the constructor shall set the x508 as null.] */
        /* SRS_ATTESTATION_MECHANISM_21_005: [The constructor shall throw ArgumentException if the provided attestation is unknown.] */
        /* SRS_ATTESTATION_MECHANISM_21_006: [If the provided attestation is instance of X509Attestation, the constructor shall store the provided x509 certificates.] */
        /* SRS_ATTESTATION_MECHANISM_21_007: [If the provided attestation is instance of X509Attestation, the constructor shall set the attestation type as X509.] */
        /* SRS_ATTESTATION_MECHANISM_21_008: [If the provided attestation is instance of X509Attestation, the constructor shall set the tpm as null.] */
        /* SRS_ATTESTATION_MECHANISM_21_010: [If the type is `TPM`, the getAttestation shall return the stored TpmAttestation.] */
        /* SRS_ATTESTATION_MECHANISM_21_011: [If the type is `X509`, the getAttestation shall return the stored X509Attestation.] */
        /* SRS_ATTESTATION_MECHANISM_21_012: [If the type is not `X509` or `TPM`, the getAttestation shall throw ProvisioningServiceClientException.] */
        /* SRS_ATTESTATION_MECHANISM_21_013: [The constructor shall throw ArgumentException if the provided AttestationMechanismType is `tpm` but the tpm attestation is null.] */
        /* SRS_ATTESTATION_MECHANISM_21_014: [If the provided AttestationMechanismType is `tpm`, the constructor shall store the provided tpm attestation.] */
        /* SRS_ATTESTATION_MECHANISM_21_015: [The constructor shall throw ArgumentException if the provided AttestationMechanismType is `x509` but the x509 attestation is null.] */
        /* SRS_ATTESTATION_MECHANISM_21_016: [If the provided AttestationMechanismType is `x509`, the constructor shall store the provided x509 attestation.] */
        /* SRS_ATTESTATION_MECHANISM_21_017: [The constructor shall throw ArgumentException if the provided AttestationMechanismType is not `tpm` or `x509`.] */
    }
}
