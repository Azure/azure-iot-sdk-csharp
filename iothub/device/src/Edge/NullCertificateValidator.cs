﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Microsoft.Azure.Devices.Client.Edge
{
    internal class NullCertificateValidator : ICertificateValidator
    {
        public static NullCertificateValidator Instance { get; } = new NullCertificateValidator();

        public Func<object, X509Certificate, X509Chain, SslPolicyErrors, bool> GetCustomCertificateValidation()
        {
            return null;
        }
    }
}
