// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    internal interface IClientPipelineBuilder
    {
        IClientPipelineBuilder With(ContinuationFactory<IDelegatingHandler> delegatingHandlerCreator);

        IDelegatingHandler Build(PipelineContext context);
    }
}