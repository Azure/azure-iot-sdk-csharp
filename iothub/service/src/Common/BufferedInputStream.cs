// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;

namespace Microsoft.Azure.Devices.Common
{
    internal class BufferedInputStream : Stream, ICloneable
    {
        private readonly BufferManagerByteArray _data;
        private readonly MemoryStream _innerStream;
        private bool _isDisposed;

        public BufferedInputStream(byte[] bytes, int bufferSize, InternalBufferManager bufferManager)
        {
            _data = new BufferManagerByteArray(bytes, bufferManager);
            _innerStream = new MemoryStream(bytes, 0, bufferSize);
        }

        private BufferedInputStream(BufferManagerByteArray data, int bufferSize)
        {
            _data = data;
            _data.AddReference();
            _innerStream = new MemoryStream(data.Bytes, 0, bufferSize);
        }

        public byte[] Buffer
        {
            get
            {
                ThrowIfDisposed();
                return _data.Bytes;
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                ThrowIfDisposed();
                return _innerStream.Length;
            }
        }

        public override long Position
        {
            get
            {
                ThrowIfDisposed();
                return _innerStream.Position;
            }

            set
            {
                ThrowIfDisposed();
                _innerStream.Position = value;
            }
        }

        public object Clone()
        {
            ThrowIfDisposed();
            return new BufferedInputStream(_data, (int)_innerStream.Length);
        }

        public override void Flush()
        {
            throw FxTrace.Exception.AsError(new NotSupportedException());
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            ThrowIfDisposed();
            return _innerStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            ThrowIfDisposed();
            return _innerStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException());
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw FxTrace.Exception.AsError(new NotSupportedException());
        }

        protected override void Dispose(bool disposing)
        {
            try
            {
                if (!_isDisposed && disposing)
                {
                    if (disposing)
                    {
                        _innerStream.Dispose();
                    }

                    _data.RemoveReference();
                    _isDisposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw FxTrace.Exception.AsError(new ObjectDisposedException("BufferedInputStream"));
            }
        }

        private sealed class BufferManagerByteArray
        {
            private volatile int references;

            public BufferManagerByteArray(byte[] bytes, InternalBufferManager bufferManager)
            {
                Bytes = bytes;
                BufferManager = bufferManager;
                references = 1;
            }

            public byte[] Bytes { get; private set; }

            private InternalBufferManager BufferManager { get;set; }

            public void AddReference()
            {
#pragma warning disable 0420
                if (Interlocked.Increment(ref references) == 1)
#pragma warning restore 0420
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(Resources.BufferAlreadyReclaimed));
                }
            }

            public void RemoveReference()
            {
                if (references > 0)
                {
#pragma warning disable 0420
                    if (Interlocked.Decrement(ref references) == 0)
#pragma warning restore 0420
                    {
                        BufferManager.ReturnBuffer(Bytes);
                        Bytes = null;
                    }
                }
            }
        }
    }
}
