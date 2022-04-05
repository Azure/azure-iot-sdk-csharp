// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal interface ITransportHandlerFactory
    {
        IDelegatingHandler Create(IPipelineContext context);
    }
}