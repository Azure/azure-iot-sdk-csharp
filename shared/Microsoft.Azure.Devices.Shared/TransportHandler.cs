// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Shared
{
    public abstract class TransportHandler : IDisposable
    {
        public TransportFallbackType TransportFallback
        {
            get;
            private set;
        }

        public TransportHandler()
        {

        }

        public TransportHandler(TransportFallbackType transportFallback)
        {
            TransportFallback = transportFallback;
        }

        public abstract void Dispose();
    }
}
