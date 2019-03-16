// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using System;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal abstract class HttpAuthStrategy
    {
        public abstract DeviceProvisioningServiceRuntimeClient CreateClient(Uri uri, HttpClientHandler httpClientHandler);

        public abstract DeviceRegistration CreateDeviceRegistration();

        public abstract void SaveCredentials(RegistrationOperationStatus status);
    }
}
