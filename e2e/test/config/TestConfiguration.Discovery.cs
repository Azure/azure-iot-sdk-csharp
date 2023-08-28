// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;
using FluentAssertions;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        public static partial class Discovery
        {
            public static string ConnectionString => GetValue("PROVISIONING_CONNECTION_STRING");

            public static string GlobalDeviceEndpoint =>
                GetValue("DPS_GLOBALDISCOVERYENDPOINT", "dev1.eastus.device.discovery.edgeprov-dev.azure.net/");

            public static string ConnectionStringInvalidServiceCertificate => GetValue("PROVISIONING_CONNECTION_STRING_INVALIDCERT", string.Empty);

            public static string GlobalDeviceEndpointInvalidServiceCertificate => GetValue("DPS_GLOBALDEVICEENDPOINT_INVALIDCERT", string.Empty);
        }
    }
}
