﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.IO;
    using System.Threading;
    using Microsoft.Azure.Devices.Client.Extensions;
#if NETMF
    using System.Collections;
#else
    using Microsoft.Azure.Devices.Client.Common.Api;
    using System.Collections.Generic;
    using Microsoft.Azure.Amqp;
#endif

    using DateTimeT = System.DateTime;

    /// <summary>
    /// The data structure represent the message that is used for interacting with IotHub.
    /// </summary>
    public sealed class Message :
        // TODO: this is a crazy mess, clean it up
#if !NETMF
        IReadOnlyIndicator,
#endif
        IDisposable
    {
        readonly object messageLock = new object();
#if NETMF
        Stream bodyStream;
#else
        volatile Stream bodyStream;
#endif
        bool disposed;
        bool ownsBodyStream;

        private const long StreamCannotSeek = -1;
        long originalStreamPosition = StreamCannotSeek;

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
        public Message()
        {
#if NETMF
            this.Properties = new Hashtable();
            this.SystemProperties = new Hashtable();
#else
            this.Properties = new ReadOnlyDictionary45<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), this);
            this.SystemProperties = new ReadOnlyDictionary45<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), this);
            this.InitializeWithStream(Stream.Null, true);
            this.serializedAmqpMessage = null;
#endif
        }

        /// <summary>
        /// Constructor which uses the argument stream as the body stream.
        /// </summary>
        /// <param name="stream">a stream which will be used as body stream.</param>
        /// <remarks>User is expected to own the disposing of the stream when using this constructor.</remarks>
        // UWP cannot expose a method with System.IO.Stream in signature. TODO: consider adding an IRandomAccessStream overload
        public Message(Stream stream)
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
        /// <param name="byteArray">a byte array which will be used to
        /// form the body stream</param>
        /// <remarks>user should treat the input byte array as immutable when
        /// sending the message.</remarks>
#if NETMF
        public Message(byte[] byteArray)
            : this(new MemoryStream(byteArray))
#else
        public Message(
            byte[] byteArray)
            : this(new MemoryStream(byteArray))
#endif
        {
            // reset the owning of the steams
            this.ownsBodyStream = true;
        }

#if !NETMF
        /// <summary>
        /// This constructor is only used in the receive path from Amqp path, 
        /// or in Cloning from a Message that has serialized.
        /// </summary>
        /// <param name="amqpMessage"></param>
        internal Message(AmqpMessage amqpMessage)
            : this()
        {
            if (amqpMessage == null)
            {
                throw Fx.Exception.ArgumentNull("amqpMessage");
            }

            MessageConverter.UpdateMessageHeaderAndProperties(amqpMessage, this);
            Stream stream = amqpMessage.BodyStream;
            this.InitializeWithStream(stream, true);
        }
#endif
        /// <summary>
        /// This constructor is only used on the Gateway http path so that 
        /// we can clean up the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ownStream"></param>
        internal Message(Stream stream, bool ownStream)
            : this(stream)
        {
            this.ownsBodyStream = ownStream;
        }

        /// <summary>
        /// [Required for two way requests] Used to correlate two-way communication. 
        /// Format: A case-sensitive string ( up to 128 char long) of ASCII 7-bit alphanumeric chars 
        /// + {'-', ':', '/', '\', '.', '+', '%', '_', '#', '*', '?', '!', '(', ')', ',', '=', '@', ';', '$', '''}. 
        /// Non-alphanumeric characters are from URN RFC.
        /// </summary>
        public string MessageId
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.MessageId) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.MessageId);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.MessageId] = value;
            }
        }

        /// <summary>
        /// [Required] Destination of the message
        /// </summary>
        public string To
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.To) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.To);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.To] = value;
            }
        }

        /// <summary>
        /// [Optional] The time when this message is considered expired
        /// </summary>
        public DateTimeT ExpiryTimeUtc
        {
            get
            {
#if NETMF
                return (DateTime)(this.GetSystemProperty(MessageSystemPropertyNames.ExpiryTimeUtc) ?? DateTime.MinValue);
#else
                return this.GetSystemProperty<DateTimeT>(MessageSystemPropertyNames.ExpiryTimeUtc);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.ExpiryTimeUtc] = value;
            }
        }

        /// <summary>
        /// Used in message responses and feedback
        /// </summary>
        public string CorrelationId
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.CorrelationId) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.CorrelationId);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.CorrelationId] = value;
            }
        }

        /// <summary>
        /// Indicates whether consumption or expiration of the message should post data to the feedback queue
        /// </summary>
        DeliveryAcknowledgement Ack
        {
            get
            {
#if NETMF
                string deliveryAckAsString = this.GetSystemProperty(MessageSystemPropertyNames.Ack) as string ?? string.Empty;
#else
                string deliveryAckAsString = this.GetSystemProperty<string>(MessageSystemPropertyNames.Ack);
#endif
                if (!deliveryAckAsString.IsNullOrWhiteSpace())
                {
                    return Utils.ConvertDeliveryAckTypeFromString(deliveryAckAsString);
                }

                return DeliveryAcknowledgement.None;
            }
            set
            {
                this.SystemProperties[MessageSystemPropertyNames.Ack] = Utils.ConvertDeliveryAckTypeToString(value);
            }
        }

        /// <summary>
        /// [Required] SequenceNumber of the received message
        /// </summary>
        public ulong SequenceNumber
        {
            get
            {
#if NETMF
                return (ulong)(this.GetSystemProperty(MessageSystemPropertyNames.SequenceNumber) ?? 0);
#else
                return this.GetSystemProperty<ulong>(MessageSystemPropertyNames.SequenceNumber);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.SequenceNumber] = value;
            }
        }

        /// <summary>
        /// [Required] LockToken of the received message
        /// </summary>
        public string LockToken
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.LockToken) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.LockToken);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.LockToken] = value;
            }
        }

        /// <summary>
        /// Time when the message was received by the server
        /// </summary>
        public DateTimeT EnqueuedTimeUtc
        {
            get
            {
#if NETMF
                return (DateTime)(this.GetSystemProperty(MessageSystemPropertyNames.EnqueuedTime) ?? DateTime.MinValue);
#else
                return this.GetSystemProperty<DateTimeT>(MessageSystemPropertyNames.EnqueuedTime);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.EnqueuedTime] = value;
            }
        }

        /// <summary>
        /// Number of times the message has been previously delivered
        /// </summary>
        public uint DeliveryCount
        {
            get
            {
#if NETMF
                return (byte)(this.GetSystemProperty(MessageSystemPropertyNames.DeliveryCount) ?? 0);
#else
                return this.GetSystemProperty<byte>(MessageSystemPropertyNames.DeliveryCount);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.DeliveryCount] = (byte)value;
            }
        }

        /// <summary>
        /// [Required in feedback messages] Used to specify the origin of messages generated by device hub. 
        /// Possible value: “{hub name}/”
        /// </summary>
        public string UserId
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.UserId) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.UserId);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.UserId] = value;
            }
        }

        /// <summary>
        /// For outgoing messages, contains the Mqtt topic that the message is being sent to
        /// For incoming messages, contains the Mqtt topic that the message arrived on
        /// </summary>
        internal string MqttTopicName { get; set; }
        
        /// <summary>
        /// Used to specify the schema of the message content.
        /// </summary>
        public string MessageSchema
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.MessageSchema) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.MessageSchema);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.MessageSchema] = value;
            }
        }

        /// <summary>
        /// Custom date property set by the originator of the message.
        /// </summary>
        public DateTimeT CreationTimeUtc
        {
            get
            {
#if NETMF
                return (DateTime)(this.GetSystemProperty(MessageSystemPropertyNames.CreationTimeUtc) ?? DateTime.MinValue);
#else
                return this.GetSystemProperty<DateTimeT>(MessageSystemPropertyNames.CreationTimeUtc);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.CreationTimeUtc] = value;
            }
        }

        /// <summary>
        /// True if the message is set as a security message
        /// </summary>
        public bool IsSecurityMessage
        {
            get
            {
#if NETMF
                return CommonConstants.SecurityMessageInterfaceId.Equals(this.GetSystemProperty(MessageSystemPropertyNames.InterfaceId), StringComparison.Ordinal);
#else
                return CommonConstants.SecurityMessageInterfaceId.Equals(this.GetSystemProperty<String>(MessageSystemPropertyNames.InterfaceId), StringComparison.Ordinal);
#endif
            }
        }

        /// <summary>
        /// Used to specify the content type of the message.
        /// </summary>
        public string ContentType
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.ContentType) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.ContentType);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.ContentType] = value;
            }
        }

        /// <summary>
        /// Specifies the input name on which the message was sent, if there was one.
        /// </summary>
        public string InputName
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.InputName) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.InputName);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.InputName] = value;
            }
        }

        /// <summary>
        /// Specifies the device Id from which this message was sent, if there is one. 
        /// </summary>
        public string ConnectionDeviceId
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.ConnectionDeviceId) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.ConnectionDeviceId);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.ConnectionDeviceId] = value;
            }
        }

        /// <summary>
        /// Specifies the module Id from which this message was sent, if there is one.
        /// </summary>
        public string ConnectionModuleId
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.ConnectionModuleId) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.ConnectionModuleId);
#endif
            }

            internal set
            {
                this.SystemProperties[MessageSystemPropertyNames.ConnectionModuleId] = value;
            }
        }

        /// <summary>
        /// Used to specify the content encoding type of the message.
        /// </summary>
        public string ContentEncoding
        {
            get
            {
#if NETMF
                return this.GetSystemProperty(MessageSystemPropertyNames.ContentEncoding) as string ?? string.Empty;
#else
                return this.GetSystemProperty<string>(MessageSystemPropertyNames.ContentEncoding);
#endif
            }

            set
            {
                this.SystemProperties[MessageSystemPropertyNames.ContentEncoding] = value;
            }
        }

        /// <summary>
        /// Gets the dictionary of user properties which are set when user send the data.
        /// </summary>
#if NETMF
        public Hashtable Properties { get; private set; }
#else
        public IDictionary<string, string> Properties { get; private set; }
#endif

        /// <summary>
        /// Gets the dictionary of system properties which are managed internally.
        /// </summary>
#if NETMF
        internal Hashtable SystemProperties { get; private set; }
#else
        internal IDictionary<string, object> SystemProperties { get; private set; }
#endif

#if !NETMF
        bool IReadOnlyIndicator.IsReadOnly
        {
            get
            {
                return Interlocked.Read(ref this.sizeInBytesCalled) == 1;
            }
        }
#endif

        public Stream BodyStream
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

#if !NETMF
        /// <summary>
        /// Gets or sets the deliveryTag which is used for server side checkpointing.
        /// </summary>
        internal ArraySegment<byte> DeliveryTag { get; set; }
#endif

        /// <summary>
        /// Dispose the current event data instance
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
        }

        /// <summary>
        /// Return the body stream of the current event data instance
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">throws if the method has been called.</exception>
        /// <exception cref="ObjectDisposedException">throws if the event data has already been disposed.</exception>
        /// <remarks>This method can only be called once and afterwards method will throw <see cref="InvalidOperationException"/>.</remarks>
        public Stream GetBodyStream()
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
        /// <exception cref="ObjectDisposedException">throws if the event data has already been disposed.</exception>
        public byte[] GetBytes()
        {
            this.ThrowIfDisposed();
            this.SetGetBodyCalled();
            if (this.bodyStream == null)
            {
#if NET451 || NETMF
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif
            }

            byte[] result;
#if !NETMF
            BufferListStream listStream;
            if ((listStream = this.bodyStream as BufferListStream) != null)
            {
                // We can trust Amqp bufferListStream.Length;
                result = new byte[listStream.Length];
                listStream.Read(result, 0, result.Length);
            }
            else
#endif
            {
                // This is just fail safe code in case we are not using the Amqp protocol.
                result = ReadFullStream(this.bodyStream);
            }

            return result;
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

        internal void ResetBody()
        {
            if (this.originalStreamPosition == StreamCannotSeek)
            {
                throw new IOException("Stream cannot seek.");
            }

            this.bodyStream.Seek(this.originalStreamPosition, SeekOrigin.Begin);
            Interlocked.Exchange(ref this.getBodyCalled, 0);

#if !NETMF
            this.serializedAmqpMessage = null;
#endif
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

        /// <summary>
        /// Sets the message as an security message
        /// </summary>
        public void SetAsSecurityMessage()
        {
            SystemProperties[MessageSystemPropertyNames.InterfaceId] = CommonConstants.SecurityMessageInterfaceId;
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

            if (this.bodyStream.CanSeek)
            {
                this.originalStreamPosition = this.bodyStream.Position;
            }
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
            MessageConverter.UpdateAmqpMessageHeadersAndProperties(message, this);
            return message;
        }
#endif

#if NETMF
        object GetSystemProperty(string key)
        {
            // .NetMF doesn't have generics so we have to resort to look for the key and return the object inside. The caller will have to figure out how to handle it
            return this.SystemProperties[key];
        }
#else
        T GetSystemProperty<T>(string key)
        {
            if (this.SystemProperties.ContainsKey(key))
            {
                return (T)this.SystemProperties[key];
            }

            return default(T);
        }
#endif

        void ThrowIfDisposed()
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
