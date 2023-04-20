// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Microsoft.Azure.Devices.Client.Transport
{
    internal sealed class TransportHandlerFactory : ITransportHandlerFactory
    {
        public IDelegatingHandler Create(PipelineContext context, IDelegatingHandler nextHandler)
        {
            IotHubClientTransportSettings transportSettings = context.IotHubClientTransportSettings;
            return transportSettings switch
            {
                IotHubClientAmqpSettings => new AmqpTransportHandler(context, nextHandler),

                IotHubClientMqttSettings => new MqttTransportHandler(context, nextHandler),

                _ => throw new InvalidOperationException($"Unsupported transport setting {context.IotHubClientTransportSettings.GetType()}")
            };
        }
    }
}
