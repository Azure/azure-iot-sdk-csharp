// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal delegate T ContinuationFactory<out T>(IPipelineContext context, IDelegatingHandler innerHandler);

    internal interface IContinuationProvider<T> where T: IDelegatingHandler
    {
        ContinuationFactory<T> ContinuationFactory { get; set; }
    }
}