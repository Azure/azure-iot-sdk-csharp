// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Provisioning.Client.Transport.Http.Models;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport.Http
{
    // TODO: Derives from DelegateHandler.
    internal abstract class HttpTransportHandler
    {
        public abstract Task<DeviceProvisioningServiceRuntimeClient> CreateClient(Uri uri);

        public abstract Task<DeviceRegistration> CreateDeviceRegistration();

        public abstract ProvisioningRegistrationResult ConvertToProvisioningRegistrationResult(DeviceRegistrationResult result);
    }
}
