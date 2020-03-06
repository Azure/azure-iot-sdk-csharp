// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DotNetty.Codecs.Mqtt.Packets;
using DotNetty.Common.Concurrency;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class PublishWorkItem : ReferenceCountedObjectContainer<PublishPacket>, ICancellable
    {
        public override PublishPacket Value { get; set; }

        public TaskCompletionSource Completion { get; set; }

        public void Cancel()
        {
            this.Completion.TrySetCanceled();
        }

        public void Abort(Exception exception)
        {
            this.Completion.TrySetException(exception);
        }
    }
}
