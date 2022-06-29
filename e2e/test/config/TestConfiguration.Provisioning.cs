// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;

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

            public static string X509GroupEnrollmentName => GetValue("DPS_X509_GROUP_ENROLLMENT_NAME");

            // This certificate is a part of the chain whose root has been verified by the Provisioning service.
            // The certificates used by the group enrollment tests are signed by this intermediate certificate.
            // Chain: Root->Intermediate1->Intermediate2
            // Certificate: Intermediate2->deviceCert
            public static string GetGroupEnrollmentIntermediatePfxCertificateBase64()
            {
                const string intermediateCert = "X509_CHAIN_INTERMEDIATE2_PFX_CERTIFICATE";
                using X509Certificate2 cert = GetBase64EncodedCertificate(intermediateCert, CertificatePassword);
                cert.NotAfter.Should().NotBeBefore(DateTime.Now, $"The X509 cert from {intermediateCert} has expired.");
                return GetValue(intermediateCert);
            }

            public static string ConnectionStringInvalidServiceCertificate => GetValue("PROVISIONING_CONNECTION_STRING_INVALIDCERT", string.Empty);

            public static string GlobalDeviceEndpointInvalidServiceCertificate => GetValue("DPS_GLOBALDEVICEENDPOINT_INVALIDCERT", string.Empty);

            //Note: Due to limitations with using VSTS Hosted agents, there is no guarantee that this hub is actually farther away
            // than the other test iot hub. As such, geolatency allocation policies cannot be tested properly
            public static string FarAwayIotHubHostName => GetValue("FAR_AWAY_IOTHUB_HOSTNAME");

            public static string CustomAllocationPolicyWebhook => GetValue("CUSTOM_ALLOCATION_POLICY_WEBHOOK");
        }
    }
}
