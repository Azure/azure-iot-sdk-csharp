// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        public static partial class Provisioning
        {
            public const string CertificatePassword = "testcertificate";

            public static string GlobalDeviceEndpoint => 
                GetValue("DPS_GLOBALDEVICEENDPOINT", "global.azure-devices-provisioning.net");

            public static string IdScope => GetValue("DPS_IDSCOPE");

            public static string TpmDeviceRegistrationId => GetValue("DPS_TPM_REGISTRATIONID");

            public static string TpmDeviceId => GetValue("DPS_TPM_DEVICEID");

            // To generate use Powershell: [System.Convert]::ToBase64String( (Get-Content .\certificate.pfx -Encoding Byte) )
            public static X509Certificate2 GetIndividualEnrollmentCertificate() 
                => GetBase64EncodedCertificate("DPS_INDIVIDUALX509_PFX_CERTIFICATE", CertificatePassword);

            public static X509Certificate2 GetGroupEnrollmentCertificate() 
                => GetBase64EncodedCertificate("DPS_GROUPX509_PFX_CERTIFICATE", CertificatePassword);

            public static X509Certificate2Collection GetGroupEnrollmentChain()
                => GetBase64EncodedCertificateCollection("DPS_GROUPX509_CERTIFICATE_CHAIN");
        }
    }
}
