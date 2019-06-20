// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.IO;
    using System.Threading;
#if NETMF
    using System.Collections;
#else
    using Microsoft.Azure.Devices.Client.Common.Api;
    using System.Collections.Generic;
#endif
    using DateTimeT = System.DateTime;
    using Microsoft.Azure.Devices.Client.Transport.AmqpIoT;

    /// <summary>
    /// The data structure represent the method response that is used for interacting with IotHub.
    /// </summary>
    public sealed class MethodResponseInternal : IDisposable
    {
        readonly object messageLock = new object();
#if NETMF
        Stream bodyStream;
#else
        volatile Stream bodyStream;
#endif
        bool disposed;
        bool ownsBodyStream;
        int getBodyCalled;
#if NETMF
        int sizeInBytesCalled;
#else
        long sizeInBytesCalled;
#endif

        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        internal MethodResponseInternal()
        {
#if !NETMF
            this.InitializeWithStream(Stream.Null, true);
#endif
        }

        /// <summary>
        /// Default constructor with no requestId and status data
        /// </summary>
        internal MethodResponseInternal(string requestId, int status)
        {
#if !NETMF
            this.InitializeWithStream(Stream.Null, true);
#endif
            this.RequestId = requestId;
            this.Status = status;
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
                this.InitializeWithStream(stream, false);
            }
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body
        /// </summary>
        /// <param name="byteArray">a byte array which will be used to form the body stream</param>
        /// <param name="requestId">the method request id corresponding to this respond</param>
        /// <param name="status">the status code of the method call</param>
#if NETMF
        internal MethodResponse(byte[] byteArray)
            : this(new MemoryStream(byteArray))
#else
        internal MethodResponseInternal(
        byte[] byteArray, string requestId, int status)
            : this(new MemoryStream(byteArray))
#endif
        {
            // reset the owning of the steams
            this.ownsBodyStream = true;
            this.RequestId = requestId;
            this.Status = status;
        }

        /// <summary>
        /// contains the response of the device client application method handler.
        /// </summary>
        internal int Status
        {
            get; set;
        }

        /// <summary>
        /// the request Id for the transport layer
        /// </summary>
        internal string RequestId
        {
            get; set;
        }

        internal Stream BodyStream
        {
            get
            {
                return this.bodyStream;
            }
        }

        /// <summary>
        /// Dispose the current method data instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
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
            this.ThrowIfDisposed();
            this.SetGetBodyCalled();
            if (this.bodyStream != null)
            {
                return this.bodyStream;
            }

#if NETMF
            return null;
#else
            return Stream.Null;
#endif
        }

        /// <summary>
        /// This methods return the body stream as a byte array
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">throws if the method has been called.</exception>
        /// <exception cref="ObjectDisposedException">throws if the method data has already been disposed.</exception>
        internal byte[] GetBytes()
        {
            this.ThrowIfDisposed();
            this.SetGetBodyCalled();
            if (this.bodyStream == null)
            {
                return new byte[] { };
            }

            // This is just fail safe code in case we are not using the Amqp protocol.
            return ReadFullStream(this.bodyStream);
        }

        // Test hook only
        internal void ResetGetBodyCalled()
        {
            Interlocked.Exchange(ref this.getBodyCalled, 0);
            if (this.bodyStream != null && this.bodyStream.CanSeek)
            {
                this.bodyStream.Seek(0, SeekOrigin.Begin);
            }
        }

        internal bool TryResetBody(long position)
        {
            if (this.bodyStream != null && this.bodyStream.CanSeek)
            {
                this.bodyStream.Seek(position, SeekOrigin.Begin);
                Interlocked.Exchange(ref this.getBodyCalled, 0);
                return true;
            }
            return false;
        }

#if NETMF
        internal bool IsBodyCalled
        {
			// A safe comparison for one that will never actually perform an exchange (maybe not necessary?)
            get { return Interlocked.CompareExchange(ref this.getBodyCalled, 9999, 9999) == 1; }
        }
#else
        internal bool IsBodyCalled => Volatile.Read(ref this.getBodyCalled) == 1;
#endif

        void SetGetBodyCalled()
        {
            if (1 == Interlocked.Exchange(ref this.getBodyCalled, 1))
            {
#if NETMF
                throw new InvalidOperationException("The message body cannot be read multiple times. To reuse it store the value after reading.");
#else
                throw Fx.Exception.AsError(new InvalidOperationException(ApiResources.MessageBodyConsumed));
#endif
            }
        }

        void SetSizeInBytesCalled()
        {
            Interlocked.Exchange(ref this.sizeInBytesCalled, 1);
        }

        void InitializeWithStream(Stream stream, bool ownsStream)
        {
            // This method should only be used in constructor because
            // this has no locking on the bodyStream.
            this.bodyStream = stream;
            this.ownsBodyStream = ownsStream;
        }

        static byte[] ReadFullStream(Stream inputStream)
        {
#if NETMF
            inputStream.Position = 0;
            byte[] buffer = new byte[inputStream.Length];

            inputStream.Read(buffer, 0, (int)inputStream.Length);

            return buffer;
#else
            using (var ms = new MemoryStream())
            {
                inputStream.CopyTo(ms);
                return ms.ToArray();
            }
#endif
        }

        internal void ThrowIfDisposed()
        {
            if (this.disposed)
            {
#if NETMF
                throw new Exception("Message disposed");
#else
                throw Fx.Exception.ObjectDisposed(ApiResources.MessageDisposed);
#endif
            }
        }

        void Dispose(bool disposing)
        {
            if (!this.disposed)
            {
                if (disposing)
                {
                    if (this.bodyStream != null && this.ownsBodyStream)
                    {
                        this.bodyStream.Dispose();
                        this.bodyStream = null;
                    }
                }

                this.disposed = true;
            }
        }
    }
}