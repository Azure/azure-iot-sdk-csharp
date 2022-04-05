// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.IO;
using System.Threading;
using Microsoft.Azure.Devices.Client.Common.Api;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the method response that is used for interacting with IotHub.
    /// </summary>
    public sealed class MethodResponseInternal : IDisposable
    {
        private volatile Stream _bodyStream;
        private bool _disposed;
        private bool _ownsBodyStream;
        private int _getBodyCalled;

        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        internal MethodResponseInternal()
        {
            InitializeWithStream(Stream.Null, true);
        }

        /// <summary>
        /// Default constructor with no requestId and status data
        /// </summary>
        internal MethodResponseInternal(string requestId, int status)
        {
            InitializeWithStream(Stream.Null, true);
            RequestId = requestId;
            Status = status;
        }

        /// <summary>
        /// Constructor which uses the argument stream as the body stream.
        /// </summary>
        /// <param name="stream">a stream which will be used as body stream.</param>
        /// <remarks>User is expected to own the disposing of the stream when using this constructor.</remarks>
        // UWP cannot expose a method with System.IO.Stream in signature. TODO: consider adding an IRandomAccessStream overload
        internal MethodResponseInternal(Stream stream)
            : this()
        {
            if (stream != null)
            {
                InitializeWithStream(stream, false);
            }
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body
        /// </summary>
        /// <param name="byteArray">a byte array which will be used to form the body stream</param>
        /// <param name="requestId">the method request id corresponding to this respond</param>
        /// <param name="status">the status code of the method call</param>
        internal MethodResponseInternal(
        byte[] byteArray, string requestId, int status)
            : this(new MemoryStream(byteArray))
        {
            // reset the owning of the steams
            _ownsBodyStream = true;
            RequestId = requestId;
            Status = status;
        }

        /// <summary>
        /// contains the response of the device client application method handler.
        /// </summary>
        internal int Status { get; set; }

        /// <summary>
        /// the request Id for the transport layer
        /// </summary>
        internal string RequestId { get; set; }

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
                throw Fx.Exception.AsError(new InvalidOperationException(ApiResources.MessageBodyConsumed));
            }
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
            using var ms = new MemoryStream();
            inputStream.CopyTo(ms);
            return ms.ToArray();
        }

        internal void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw Fx.Exception.ObjectDisposed(ApiResources.MessageDisposed);
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
