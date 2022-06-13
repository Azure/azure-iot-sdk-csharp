// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static partial class Provisioning
        {
            public static string CertificatePassword => GetValue("DPS_X509_PFX_CERTIFICATE_PASSWORD");

            public static string ConnectionString => GetValue("PROVISIONING_CONNECTION_STRING");

            public static string GlobalDeviceEndpoint =>
                GetValue("DPS_GLOBALDEVICEENDPOINT", "global.azure-devices-provisioning.net");

            public static string IdScope => GetValue("DPS_IDSCOPE");

            // To generate use Powershell: [System.Convert]::ToBase64String( (Get-Content .\certificate.pfx -Encoding Byte) )
            public static X509Certificate2 GetIndividualEnrollmentCertificate()
                => GetBase64EncodedCertificate("DPS_INDIVIDUALX509_PFX_CERTIFICATE", CertificatePassword);

            public static X509Certificate2 GetGroupEnrollmentCertificate()
                => GetBase64EncodedCertificate("DPS_X509_GROUP_ENROLLMENT_DEVICE_PFX_CERTIFICATE", CertificatePassword);

            public static X509Certificate2Collection GetGroupEnrollmentChain()
                => GetBase64EncodedCertificateCollection("DPS_GROUPX509_CERTIFICATE_CHAIN");

            public static string ConnectionStringInvalidServiceCertificate => GetValue("PROVISIONING_CONNECTION_STRING_INVALIDCERT", string.Empty);

            public static string GlobalDeviceEndpointInvalidServiceCertificate => GetValue("DPS_GLOBALDEVICEENDPOINT_INVALIDCERT", string.Empty);

            //Note: Due to limitations with using VSTS Hosted agents, there is no guarantee that this hub is actually farther away
            // than the other test iot hub. As such, geolatency allocation policies cannot be tested properly
            public static string FarAwayIotHubHostName => GetValue("FAR_AWAY_IOTHUB_HOSTNAME");

            public static string CustomAllocationPolicyWebhook => GetValue("CUSTOM_ALLOCATION_POLICY_WEBHOOK");
        }
    }
}
