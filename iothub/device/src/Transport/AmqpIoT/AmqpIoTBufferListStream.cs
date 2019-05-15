// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices.Client.Transport.AmqpIoT
{
    internal class AmqpIoTBufferListStream : Stream, IAmqpIoTBufferListStream, IDisposable
    {
        private BufferListStream _bufferListStream;

        public AmqpIoTBufferListStream(IList<ArraySegment<byte>> arraySegments)
        {
            _bufferListStream = new BufferListStream(arraySegments);
        }

        public override bool CanRead => _bufferListStream.CanRead;

        public override bool CanSeek => _bufferListStream.CanSeek;

        public override bool CanWrite => _bufferListStream.CanWrite;

        public override long Length => _bufferListStream.Length;

        public override long Position { get => _bufferListStream.Position; set => _bufferListStream.Position = value; }

        public override void Flush() => _bufferListStream.Flush();

        public override int Read(byte[] buffer, int offset, int count)
        {
            return _bufferListStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return _bufferListStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            _bufferListStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            _bufferListStream.Write(buffer, offset, count);
        }

        public void Dispose() => _bufferListStream.Dispose();
    }
}
