// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    public abstract class ProvisioningTransportClient : IDisposable
    {
        // TODO: Add DelegatingHandler pipeline.
        
        public abstract Task<ProvisioningRegistrationResult> RegisterAsync(
            string globalDeviceEndpoint,
            string idScope,
            ProvisioningSecurityClient securityClient,
            CancellationToken cancellationToken);

        public virtual Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        protected virtual void Dispose(bool disposing) { }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~ProvisioningTransportClient()
        {
            Dispose(false);
        }
    }
}
