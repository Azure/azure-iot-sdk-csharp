// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Threading;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the method request coming from the IotHub.
    /// </summary>
    public sealed class MethodRequestInternal : IDisposable
    {
        private volatile Stream _bodyStream;
        private bool _disposed;
        private bool _ownsBodyStream;
        private int _getBodyCalled;
        private long _sizeInBytesCalled;

        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        internal MethodRequestInternal(CancellationToken cancellationToken)
        {
            InitializeWithStream(Stream.Null, true);
            CancellationToken = cancellationToken;
        }

        /// <summary>
        /// This constructor is only used in the receive path from Amqp path,
        /// or in Cloning from a Message that has serialized.
        /// </summary>

        internal MethodRequestInternal(string name, string requestId, Stream bodyStream, CancellationToken cancellationToken)
            : this(cancellationToken)
        {
            Name = name;
            RequestId = requestId;
            Stream stream = bodyStream;
            InitializeWithStream(stream, false);
        }

        internal CancellationToken CancellationToken { get; private set; }

        /// <summary>
        /// Property indicating the method name for this instance
        /// </summary>
        internal string Name { get; private set; }

        /// <summary>
        /// the request Id for the transport layer
        /// </summary>
        internal string RequestId { get; private set; }

        internal Stream BodyStream => _bodyStream;

        /// <summary>
        /// Dispose the current method data instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        /// <summary>
        /// Return the body stream of the current method data instance
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">throws if the method has been called.</exception>
        /// <exception cref="ObjectDisposedException">throws if the method data has already been disposed.</exception>
        /// <remarks>This method can only be called once and afterwards method will throw <see cref="InvalidOperationException"/>.</remarks>
        internal Stream GetBodyStream()
        {
            ThrowIfDisposed();
            SetGetBodyCalled();
            if (_bodyStream != null)
            {
                return _bodyStream;
            }

            return Stream.Null;
        }

        /// <summary>
        /// This methods return the body stream as a byte array
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">throws if the method has been called.</exception>
        /// <exception cref="ObjectDisposedException">throws if the method data has already been disposed.</exception>
        internal byte[] GetBytes()
        {
            ThrowIfDisposed();
            SetGetBodyCalled();
            if (_bodyStream == null)
            {
                return Array.Empty<byte>();
            }

            // This is just fail safe code in case we are not using the Amqp protocol.
            return ReadFullStream(_bodyStream);
        }

        // Test hook only
        internal void ResetGetBodyCalled()
        {
            Interlocked.Exchange(ref _getBodyCalled, 0);
            if (_bodyStream != null && _bodyStream.CanSeek)
            {
                _bodyStream.Seek(0, SeekOrigin.Begin);
            }
        }

        internal bool TryResetBody(long position)
        {
            if (_bodyStream != null && _bodyStream.CanSeek)
            {
                _bodyStream.Seek(position, SeekOrigin.Begin);
                Interlocked.Exchange(ref _getBodyCalled, 0);
                return true;
            }
            return false;
        }

        internal bool IsBodyCalled => Volatile.Read(ref _getBodyCalled) == 1;

        private void SetGetBodyCalled()
        {
            if (1 == Interlocked.Exchange(ref _getBodyCalled, 1))
            {
                throw Fx.Exception.AsError(new InvalidOperationException(Common.Api.ApiResources.MessageBodyConsumed));
            }
        }

        private void SetSizeInBytesCalled()
        {
            Interlocked.Exchange(ref _sizeInBytesCalled, 1);
        }

        private void InitializeWithStream(Stream stream, bool ownsStream)
        {
            // This method should only be used in constructor because
            // this has no locking on the bodyStream.
            _bodyStream = stream;
            _ownsBodyStream = ownsStream;
        }

        private static byte[] ReadFullStream(Stream inputStream)
        {
            using (var ms = new MemoryStream())
            {
                inputStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw Fx.Exception.ObjectDisposed(Common.Api.ApiResources.MessageDisposed);
            }
        }

        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    if (_bodyStream != null && _ownsBodyStream)
                    {
                        _bodyStream.Dispose();
                        _bodyStream = null;
                    }
                }

                _disposed = true;
            }
        }
    }
}
