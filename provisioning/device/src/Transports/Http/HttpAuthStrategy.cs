// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal abstract class HttpAuthStrategy
    {
        public static readonly TimeSpan TimeoutConstant = TimeSpan.FromSeconds(90);

        public abstract DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri, HttpClientHandler httpClientHandler);

        public abstract DeviceRegistrationHttp CreateDeviceRegistration();

        public abstract void SaveCredentials(RegistrationOperationStatus status);
    }
}
