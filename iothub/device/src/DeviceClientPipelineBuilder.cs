// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Collections.Generic;

    class DeviceClientPipelineBuilder : IDeviceClientPipelineBuilder
    {
        readonly List<ContinuationFactory<IDelegatingHandler>> pipeline = new List<ContinuationFactory<IDelegatingHandler>>();

        public IDeviceClientPipelineBuilder With(ContinuationFactory<IDelegatingHandler> delegatingHandlerCreator)
        {
            this.pipeline.Add(delegatingHandlerCreator);
            return this;
        }

        public IDelegatingHandler Build(IPipelineContext context)
        {
            if (this.pipeline.Count == 0)
            {
                throw new InvalidOperationException("Pipeline is not setup");
            }

            IDelegatingHandler root = this.WrapContinuationFactory(0)(context);
            return root;
        }

        ContinuationFactory<IDelegatingHandler> WrapContinuationFactory(int currentId)
        {
            ContinuationFactory<IDelegatingHandler> current = this.pipeline[currentId];
            if (currentId == this.pipeline.Count - 1)
            {
                return current;
            }
            ContinuationFactory<IDelegatingHandler> next = this.WrapContinuationFactory(currentId + 1);
            ContinuationFactory<IDelegatingHandler> currentHandlerFactory = current;
            current = ctx =>
            {
                IDelegatingHandler delegatingHandler = currentHandlerFactory(ctx);
                delegatingHandler.ContinuationFactory = next;
                return delegatingHandler;
            };
            return current;
        }
    }
}