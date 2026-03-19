// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client
{
    internal interface ICertificateValidator : IDisposable
    {
        internal Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> GetCustomCertificateValidation();
    }
}
