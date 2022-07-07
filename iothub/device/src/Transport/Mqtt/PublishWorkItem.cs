// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DotNetty.Codecs.Mqtt.Packets;
using TaskCompletionSource = Microsoft.Azure.Devices.TaskCompletionSource;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class PublishWorkItem : ReferenceCountedObjectContainer<PublishPacket>, ICancellable
    {
        public override PublishPacket Value { get; set; }

        public TaskCompletionSource Completion { get; set; }

        public void Cancel()
        {
            Completion.TrySetCanceled();
        }

        public void Abort(Exception exception)
        {
            Completion.TrySetException(exception);
        }
    }
}
