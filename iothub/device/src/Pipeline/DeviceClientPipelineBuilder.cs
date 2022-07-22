// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    internal class DeviceClientPipelineBuilder : IDeviceClientPipelineBuilder
    {
        private readonly List<ContinuationFactory<IDelegatingHandler>> _pipeline = new();

        public IDeviceClientPipelineBuilder With(ContinuationFactory<IDelegatingHandler> delegatingHandlerCreator)
        {
            _pipeline.Add(delegatingHandlerCreator);
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

            // Initializes all handlers except the last one: the transport. 
            // That is dynamically initialized by the ProtocolRoutingDelegatingHandler.
            for (int i = _pipeline.Count - 2; i >= 0; i--)
            {
                ContinuationFactory<IDelegatingHandler> currentFactory = _pipeline[i];
                ContinuationFactory<IDelegatingHandler> nextFactory = _pipeline[i + 1];
                currentHandler = currentFactory(context, nextHandler);
                currentHandler.ContinuationFactory = nextFactory;

                nextHandler = currentHandler;
            }

            return currentHandler;
        }
    }
}
