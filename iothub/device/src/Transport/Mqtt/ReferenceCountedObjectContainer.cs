// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNetty.Common;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal abstract class ReferenceCountedObjectContainer<T> : IReferenceCounted where T : IReferenceCounted
    {
        public abstract T Value { get; set; }

        public IReferenceCounted Retain()
        {
            return Value.Retain();
        }

        public IReferenceCounted Retain(int increment)
        {
            return Value.Retain(increment);
        }

        public IReferenceCounted Touch()
        {
            return Value.Touch();
        }

        public IReferenceCounted Touch(object hint)
        {
            return Value.Touch(hint);
        }

        public bool Release()
        {
            return Value.Release();
        }

        public bool Release(int decrement)
        {
            return Value.Release(decrement);
        }

        public int ReferenceCount => Value.ReferenceCount;
    }
}
