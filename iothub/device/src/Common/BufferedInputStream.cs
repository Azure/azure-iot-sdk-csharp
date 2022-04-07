// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using Microsoft.Azure.Devices.Client.Common;
using System;
using System.IO;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    internal class BufferedInputStream : Stream, ICloneable
    {
        private readonly BufferManagerByteArray _data;
        private readonly MemoryStream _innerStream;
        private bool _disposed;

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

        // Note: this is the old style async model (APM) that we don't need to support. It is not supported in UWP
        // I'm leaving it in place for the code owners to review and decide. ArturL 8/14/15

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            ThrowIfDisposed();
            return new CompletedAsyncResultT<int>(_innerStream.Read(buffer, offset, count), callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult)
        {
            return CompletedAsyncResultT<int>.End(asyncResult);
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
                if (!_disposed && disposing)
                {
                    if (disposing)
                    {
                        _innerStream.Dispose();
                    }

                    _data.RemoveReference();
                    _disposed = true;
                }
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw FxTrace.Exception.AsError(new ObjectDisposedException("BufferedInputStream"));
            }
        }

        private sealed class BufferManagerByteArray
        {
            private volatile int _references;

            public BufferManagerByteArray(byte[] bytes, InternalBufferManager bufferManager)
            {
                Bytes = bytes;
                BufferManager = bufferManager;
                _references = 1;
            }

            public byte[] Bytes
            {
                get;
                private set;
            }

            private InternalBufferManager BufferManager
            {
                get;
                set;
            }

            public void AddReference()
            {
#pragma warning disable 0420
                if (Interlocked.Increment(ref _references) == 1)
#pragma warning restore 0420
                {
                    throw FxTrace.Exception.AsError(new InvalidOperationException(Resources.BufferAlreadyReclaimed));
                }
            }

            public void RemoveReference()
            {
                if (_references > 0)
                {
#pragma warning disable 0420
                    if (Interlocked.Decrement(ref _references) == 0)
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
