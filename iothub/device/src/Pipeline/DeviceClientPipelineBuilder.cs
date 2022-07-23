// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    internal class DeviceClientPipelineBuilder : IDeviceClientPipelineBuilder
    {
        private readonly List<ContinuationFactory<IDelegatingHandler>> _pipeline = new();

        public IDeviceClientPipelineBuilder With(ContinuationFactory<IDelegatingHandler> handlerCreator)
        {
            _pipeline.Add(handlerCreator);
            return this;
        }

        public IDelegatingHandler Build(PipelineContext context)
        {
            if (_pipeline.Count == 0)
            {
                throw new InvalidOperationException("Pipeline is not setup");
            }

            IDelegatingHandler nextHandler = null;
            IDelegatingHandler currentHandler = null;
            ContinuationFactory<IDelegatingHandler> nextFactory = null;

            // Initializes all handlers in reverse order, so each new one added on links to the
            // logical next one in the singly-linked list.
            for (int i = _pipeline.Count - 1; i >= 0; i--)
            {
                ContinuationFactory<IDelegatingHandler> currentFactory = _pipeline[i];
                currentHandler = currentFactory(context, nextHandler);
                currentHandler.ContinuationFactory = nextFactory;

                nextHandler = currentHandler;
                nextFactory = currentFactory;
            }

            return currentHandler;
        }
    }
}
