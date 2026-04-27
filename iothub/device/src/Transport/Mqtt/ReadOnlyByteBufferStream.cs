// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using DotNetty.Buffers;
using DotNetty.Common.Utilities;

namespace Microsoft.Azure.Devices.Client.Transport.Mqtt
{
    internal sealed class ReadOnlyByteBufferStream : Stream
    {
        private readonly IByteBuffer _buffer;
        private bool _releaseReferenceOnClosure;

        public ReadOnlyByteBufferStream(IByteBuffer buffer, bool releaseReferenceOnClosure)
        {
            _buffer = buffer;
            _releaseReferenceOnClosure = releaseReferenceOnClosure;
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length => throw new NotSupportedException();

        public override long Position
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        public override int Read(byte[] output, int offset, int count)
        {
            int read = Math.Min(count - offset, _buffer.ReadableBytes);
            _ = _buffer.ReadBytes(output, offset, read);
            return read;
        }

        public override void Flush()
        {
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (_releaseReferenceOnClosure)
            {
                _releaseReferenceOnClosure = false;
                if (disposing)
                {
                    _ = _buffer.Release();
                }
                else
                {
                    ReferenceCountUtil.SafeRelease(_buffer);
                }
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] input, int offset, int count)
        {
            throw new NotSupportedException();
        }
    }
}
