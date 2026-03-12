// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class TestConfiguration
    {
        private static string GetValue(string envName, string defaultValue = null)
        {
            string envValue = Environment.GetEnvironmentVariable(envName);
            if (string.IsNullOrWhiteSpace(envValue))
            {
                return defaultValue ?? throw new InvalidOperationException($"Configuration missing: {envName}");
            }

            return Environment.ExpandEnvironmentVariables(envValue);
        }

        private static X509Certificate2 GetBase64EncodedCertificate(string envName, string password = null, string defaultValue = null)
        {
            string certBase64 = GetValue(envName, defaultValue);

            if (certBase64 == null)
            {
                certBase64 = defaultValue ?? throw new InvalidOperationException($"Configuration missing: {envName}");
            }

            Byte[] buff = Convert.FromBase64String(certBase64);

            if (password == null)
            {
                return new X509Certificate2(buff);
            }
            else
            {
                return new X509Certificate2(buff, password);
            }
        }
    }
}
