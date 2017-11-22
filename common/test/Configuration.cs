// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static partial class Configuration
    {
        private static string GetValue(string envName, string defaultValue=null)
        {
            string envValue = Environment.GetEnvironmentVariable(envName);

            if (string.IsNullOrWhiteSpace(envValue))
            {
                return defaultValue;
            }

            return Environment.ExpandEnvironmentVariables(envValue);
        }

        private static Uri GetUriValue(string envName, Uri defaultValue=null)
        {
            string envValue = GetValue(envName, null);

            if (envValue == null)
            {
                return defaultValue;
            }

            return new Uri(envValue);
        }

        // To generate environment variables value use 
        // [Convert]::ToBase64String((Get-Content myFileName -Encoding Byte)).

        private static X509Certificate2 GetBase64EncodedCertificate(string envName, string password=null, string defaultValue=null)
        {
            string certBase64 = GetValue(envName, null);

            if (certBase64 == null)
            {
                certBase64 = defaultValue;
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

        private static X509Certificate2Collection GetBase64EncodedCertificateCollection(
            string envName, 
            string defaultValue = null)
        {
            string certBase64 = GetValue(envName, null);

            if (certBase64 == null)
            {
                certBase64 = defaultValue;
            }

            Byte[] buff = Convert.FromBase64String(certBase64);
            var collection = new X509Certificate2Collection();
            collection.Import(buff);
            return collection;
        }
    }
}
