﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Devices.Client.Exceptions;
    using Microsoft.Azure.Devices.Client.Extensions;
    using Microsoft.Azure.Amqp;
#if WINDOWS_UWP
    using System.Collections.Generic;
    using Microsoft.Azure.Devices.Client.Common.Api;
#elif NETMF
    using System.Collections;
#elif PCL
    using System.Collections.Generic;
#else
    // Full .NET Framework
    using Microsoft.Azure.Devices.Client.Common.Api;
    using System.Collections.Generic;
    using Microsoft.Azure.Amqp;
#endif

#if WINDOWS_UWP || PCL
    using DateTimeT = System.DateTimeOffset;
#else
    using DateTimeT = System.DateTime;
#endif

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

#if !NETMF
        AmqpMessage serializedAmqpMessage;
#endif

        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        internal MethodResponseInternal()
        {
#if !NETMF
            this.InitializeWithStream(Stream.Null, true);
#endif
            this.serializedAmqpMessage = null;
        }

        /// <summary>
        /// Default constructor with no requestId and status data
        /// </summary>
        internal MethodResponseInternal(string requestId, int status)
        {
#if !NETMF
            this.InitializeWithStream(Stream.Null, true);
#endif
            this.serializedAmqpMessage = null;
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
        internal MethodResponseInternal([System.Runtime.InteropServices.WindowsRuntime.ReadOnlyArrayAttribute] byte[] byteArray, string requestId, int status)
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

#if !NETMF
        internal AmqpMessage SerializedAmqpMessage
        {
            get
            {
                lock (this.messageLock)
                {
                    return this.serializedAmqpMessage;
                }
            }
        }
#endif

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

#if !NETMF
            BufferListStream listStream;
            if ((listStream = this.bodyStream as BufferListStream) != null)
            {
                // We can trust Amqp bufferListStream.Length;
                byte[] bytes = new byte[listStream.Length];
                listStream.Read(bytes, 0, bytes.Length);
                return bytes;
            }
#endif

            // This is just fail safe code in case we are not using the Amqp protocol.
            return ReadFullStream(this.bodyStream);
        }

#if !NETMF
        internal AmqpMessage ToAmqpMessage(bool setBodyCalled = true)
        {
            this.ThrowIfDisposed();
            if (this.serializedAmqpMessage == null)
            {
                lock (this.messageLock)
                {
                    if (this.serializedAmqpMessage == null)
                    {
                        // Interlocked exchange two variable does allow for a small period 
                        // where one is set while the other is not. Not sure if it is worth
                        // correct this gap. The intention of setting this two variable is
                        // so that GetBody should not be called and all Properties are
                        // readonly because the amqpMessage has been serialized.

                        this.SetSizeInBytesCalled();
                        if (this.bodyStream == null)
                        {
                            this.serializedAmqpMessage = AmqpMessage.Create();
                        }
                        else
                        {
                            this.serializedAmqpMessage = AmqpMessage.Create(this.bodyStream, false);
                            this.SetGetBodyCalled();
                        }
                        this.serializedAmqpMessage = this.PopulateAmqpMessageForSend(this.serializedAmqpMessage);
                    }
                }
            }

            return this.serializedAmqpMessage;
        }
#endif
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
#if !NETMF
                this.serializedAmqpMessage = null;
#endif
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
#if NETMF || PCL
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

#if !NETMF
        AmqpMessage PopulateAmqpMessageForSend(AmqpMessage message)
        {
            MethodConverter.PopulateAmqpMessageFromMethodResponse(message, this);
            return message;
        }
#endif

        void ThrowIfDisposed()
        {
            if (this.disposed)
            {
#if NETMF || PCL
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
#if !NETMF
                    if (this.serializedAmqpMessage != null)
                    {
                        // in the receive scenario, this.bodyStream is a reference
                        // to serializedAmqpMessage.BodyStream, and we assume disposing
                        // the amqpMessage will dispose the body stream so we don't
                        // need to dispose bodyStream twice.
                        this.serializedAmqpMessage.Dispose();
                        this.bodyStream = null;
                    }
                    else
#endif
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