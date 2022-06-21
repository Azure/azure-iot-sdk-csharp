// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using DotNetty.Codecs.Mqtt.Packets;

#if NET5_0_OR_GREATER
using TaskCompletionSource = System.Threading.Tasks.TaskCompletionSource;
#else
using TaskCompletionSource = Microsoft.Azure.Devices.TaskCompletionSource;
#endif

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
