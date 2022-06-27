// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.Contracts;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;

namespace Microsoft.Azure.Devices.Client
{
    internal class ClientWebSocketChannelConfig : IChannelConfiguration
    {
        public TimeSpan ConnectTimeout { get; set; }

        public int WriteSpinCount { get; set; }

        public IByteBufferAllocator Allocator { get; set; }

        public IRecvByteBufAllocator RecvByteBufAllocator { get; set; }

        public bool AutoRead { get; set; }

        public int WriteBufferHighWaterMark { get; set; }

        public int WriteBufferLowWaterMark { get; set; }

        public IMessageSizeEstimator MessageSizeEstimator { get; set; }

        public T GetOption<T>(ChannelOption<T> option)
        {
            Contract.Requires(option != null);

            if (ChannelOption.ConnectTimeout.Equals(option))
            {
                return (T)(object)ConnectTimeout; // no boxing will happen, compiler optimizes away such casts
            }
            if (ChannelOption.WriteSpinCount.Equals(option))
            {
                return (T)(object)WriteSpinCount;
            }
            if (ChannelOption.Allocator.Equals(option))
            {
                return (T)Allocator;
            }
            if (ChannelOption.RcvbufAllocator.Equals(option))
            {
                return (T)RecvByteBufAllocator;
            }
            if (ChannelOption.AutoRead.Equals(option))
            {
                return (T)(object)AutoRead;
            }
            if (ChannelOption.WriteBufferHighWaterMark.Equals(option))
            {
                return (T)(object)WriteBufferHighWaterMark;
            }
            if (ChannelOption.WriteBufferLowWaterMark.Equals(option))
            {
                return (T)(object)WriteBufferLowWaterMark;
            }
            if (ChannelOption.MessageSizeEstimator.Equals(option))
            {
                return (T)MessageSizeEstimator;
            }
            return default;
        }

        public bool SetOption(ChannelOption option, object value) => option.Set(this, value);

        public bool SetOption<T>(ChannelOption<T> option, T value)
        {
            if (ChannelOption.ConnectTimeout.Equals(option))
            {
                ConnectTimeout = (TimeSpan)(object)value;
            }
            else if (ChannelOption.WriteSpinCount.Equals(option))
            {
                WriteSpinCount = (int)(object)value;
            }
            else if (ChannelOption.Allocator.Equals(option))
            {
                Allocator = (IByteBufferAllocator)value;
            }
            else if (ChannelOption.RcvbufAllocator.Equals(option))
            {
                RecvByteBufAllocator = (IRecvByteBufAllocator)value;
            }
            else if (ChannelOption.AutoRead.Equals(option))
            {
                AutoRead = (bool)(object)value;
            }
            else if (ChannelOption.WriteBufferHighWaterMark.Equals(option))
            {
                WriteBufferHighWaterMark = (int)(object)value;
            }
            else if (ChannelOption.WriteBufferLowWaterMark.Equals(option))
            {
                WriteBufferLowWaterMark = (int)(object)value;
            }
            else if (ChannelOption.MessageSizeEstimator.Equals(option))
            {
                MessageSizeEstimator = (IMessageSizeEstimator)value;
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
