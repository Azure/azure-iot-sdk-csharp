// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The data structure represent the message that is used for interacting with IotHub.
    /// </summary>
    public sealed class Message : IDisposable, IReadOnlyIndicator
    {
        private readonly SemaphoreSlim _amqpMessageSemaphore = new SemaphoreSlim(1, 1);
        private volatile Stream _bodyStream;
        private AmqpMessage _serializedAmqpMessage;
        private bool _disposed;
        private bool _ownsBodyStream;
        private int _getBodyCalled;
        private long _sizeInBytesCalled;

        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        public Message()
        {
            Properties = new ReadOnlyDictionary45<string, string>(new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase), this);
            SystemProperties = new ReadOnlyDictionary45<string, object>(new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), this);
            InitializeWithStream(Stream.Null, true);
            _serializedAmqpMessage = null;
        }

        /// <summary>
        /// Constructor which uses the argument stream as the body stream.
        /// </summary>
        /// <param name="stream">a stream which will be used as body stream.</param>
        /// <remarks>User is expected to own the disposing of the stream when using this constructor.</remarks>
        public Message(Stream stream)
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
        /// <param name="byteArray">a byte array which will be used to
        /// form the body stream</param>
        /// <remarks>user should treat the input byte array as immutable when
        /// sending the message.</remarks>
        public Message(byte[] byteArray)
            : this(new MemoryStream(byteArray))
        {
            // reset the owning of the steams
            _ownsBodyStream = true;
        }

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
                throw Fx.Exception.ArgumentNull(nameof(amqpMessage));
            }

            MessageConverter.UpdateMessageHeaderAndProperties(amqpMessage, this);
            Stream stream = amqpMessage.BodyStream;
            InitializeWithStream(stream, true);
        }

        /// <summary>
        /// This constructor is only used on the Gateway http path so that
        /// we can clean up the stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="ownStream"></param>
        internal Message(Stream stream, bool ownStream)
            : this(stream)
        {
            _ownsBodyStream = ownStream;
        }

        /// <summary>
        /// [Required for two way requests] Used to correlate two-way communication.
        /// Format: A case-sensitive string ( up to 128 char long) of ASCII 7-bit alphanumeric chars
        /// + {'-', ':', '/', '\', '.', '+', '%', '_', '#', '*', '?', '!', '(', ')', ',', '=', '@', ';', '$', '''}.
        /// Non-alphanumeric characters are from URN RFC.
        /// </summary>
        /// <remarks>
        /// If this value is not supplied by the user, the service client will set this to a new GUID.
        /// </remarks>
        public string MessageId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.MessageId) ?? Guid.NewGuid().ToString();
            set => SystemProperties[MessageSystemPropertyNames.MessageId] = value;
        }

        /// <summary>
        /// [Required] Destination of the message
        /// </summary>
        public string To
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.To);
            set => SystemProperties[MessageSystemPropertyNames.To] = value;
        }

        /// <summary>
        /// [Optional] The time when this message is considered expired
        /// </summary>
        public DateTime ExpiryTimeUtc
        {
            get => GetSystemProperty<DateTime>(MessageSystemPropertyNames.ExpiryTimeUtc);
            set => SystemProperties[MessageSystemPropertyNames.ExpiryTimeUtc] = value;
        }

        /// <summary>
        /// A string property in a response message that typically contains the MessageId of the request, in request-reply patterns.
        /// </summary>
        /// <remarks>
        /// If this value is not supplied by the user, the service client will set this to <see cref="MessageId"/>.
        /// </remarks>
        public string CorrelationId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.CorrelationId) ?? MessageId;
            set => SystemProperties[MessageSystemPropertyNames.CorrelationId] = value;
        }

        /// <summary>
        /// Used in cloud-to-device messages to request IoT Hub to generate feedback messages as a result of the consumption of the message by the device.
        /// </summary>
        /// <remarks>
        /// Possible values:
        /// <para>none (default): no feedback message is generated.</para>
        /// <para>positive: receive a feedback message if the message was completed.</para>
        /// <para>negative: receive a feedback message if the message expired (or maximum delivery count was reached) without being completed by the device.</para>
        /// <para>full: both positive and negative.</para>
        /// </remarks>
        [SuppressMessage("Microsoft.Design", "CA1065:DoNotRaiseExceptionsInUnexpectedLocations", Justification = "This should never happen. If it does, the client should crash.")]
        public DeliveryAcknowledgement Ack
        {
            get
            {
                string deliveryAckAsString = GetSystemProperty<string>(MessageSystemPropertyNames.Ack);

                if (!string.IsNullOrWhiteSpace(deliveryAckAsString))
                {
                    return deliveryAckAsString switch
                    {
                        "none" => DeliveryAcknowledgement.None,
                        "positive" => DeliveryAcknowledgement.PositiveOnly,
                        "negative" => DeliveryAcknowledgement.NegativeOnly,
                        "full" => DeliveryAcknowledgement.Full,
                        _ => throw new IotHubException("Invalid Delivery Ack mode"),
                    };
                }

                return DeliveryAcknowledgement.None;
            }
            set
            {
                string valueToSet = value switch
                {
                    DeliveryAcknowledgement.None => "none",
                    DeliveryAcknowledgement.PositiveOnly => "positive",
                    DeliveryAcknowledgement.NegativeOnly => "negative",
                    DeliveryAcknowledgement.Full => "full",
                    _ => throw new IotHubException("Invalid Delivery Ack mode"),
                };
                SystemProperties[MessageSystemPropertyNames.Ack] = valueToSet;
            }
        }

        /// <summary>
        /// [Required] SequenceNumber of the received message
        /// </summary>
        internal ulong SequenceNumber
        {
            get => GetSystemProperty<ulong>(MessageSystemPropertyNames.SequenceNumber);
            set => SystemProperties[MessageSystemPropertyNames.SequenceNumber] = value;
        }

        /// <summary>
        /// [Required] LockToken of the received message
        /// </summary>
        public string LockToken
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.LockToken);
            set => SystemProperties[MessageSystemPropertyNames.LockToken] = value;
        }

        /// <summary>
        /// Time when the message was received by the server
        /// </summary>
        internal DateTime EnqueuedTimeUtc
        {
            get => GetSystemProperty<DateTime>(MessageSystemPropertyNames.EnqueuedTime);
            set => SystemProperties[MessageSystemPropertyNames.EnqueuedTime] = value;
        }

        /// <summary>
        /// Number of times the message has been previously delivered
        /// </summary>
        internal uint DeliveryCount
        {
            get => GetSystemProperty<byte>(MessageSystemPropertyNames.DeliveryCount);
            set => SystemProperties[MessageSystemPropertyNames.DeliveryCount] = (byte)value;
        }

        /// <summary>
        /// [Required in feedback messages] Used to specify the origin of messages generated by device hub.
        /// Possible value: “{hub name}/”
        /// </summary>
        public string UserId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.UserId);
            set => SystemProperties[MessageSystemPropertyNames.UserId] = value;
        }

        /// <summary>
        /// Used to specify the schema of the message content.
        /// </summary>
        public string MessageSchema
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.MessageSchema);
            set => SystemProperties[MessageSystemPropertyNames.MessageSchema] = value;
        }

        /// <summary>
        /// Custom date property set by the originator of the message.
        /// </summary>
        public DateTime CreationTimeUtc
        {
            get => GetSystemProperty<DateTime>(MessageSystemPropertyNames.CreationTimeUtc);
            set => SystemProperties[MessageSystemPropertyNames.CreationTimeUtc] = value;
        }

        /// <summary>
        /// Used to specify the content type of the message.
        /// </summary>
        public string ContentType
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ContentType);
            set => SystemProperties[MessageSystemPropertyNames.ContentType] = value;
        }

        /// <summary>
        /// Used to specify the content encoding type of the message.
        /// </summary>
        public string ContentEncoding
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ContentEncoding);
            set => SystemProperties[MessageSystemPropertyNames.ContentEncoding] = value;
        }

        /// <summary>
        /// Gets the dictionary of user properties which are set when user send the data.
        /// </summary>
        public IDictionary<string, string> Properties { get; private set; }

        /// <summary>
        /// Gets the dictionary of system properties which are managed internally.
        /// </summary>
        internal IDictionary<string, object> SystemProperties { get; private set; }

        bool IReadOnlyIndicator.IsReadOnly => Interlocked.Read(ref _sizeInBytesCalled) == 1;

        internal Stream BodyStream => _bodyStream;

        internal AmqpMessage SerializedAmqpMessage
        {
            get
            {
                try
                {
                    _amqpMessageSemaphore.Wait();
                    return _serializedAmqpMessage;
                }
                finally
                {
                    _amqpMessageSemaphore.Release();
                }
            }
        }

        /// <summary>
        /// Gets or sets the deliveryTag which is used for server side checkpointing.
        /// </summary>
        internal ArraySegment<byte> DeliveryTag { get; set; }

        /// <summary>
        /// Makes a clone of the current event data instance.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="ObjectDisposedException">throws if the event data has already been disposed.</exception>
        public Message Clone()
        {
            ThrowIfDisposed();
            if (_serializedAmqpMessage != null)
            {
                return new Message(_serializedAmqpMessage);
            }

            var message = new Message();
            if (_bodyStream != null)
            {
                // The new Message always owns the cloned stream.
                message = new Message(CloneStream(_bodyStream))
                {
                    _ownsBodyStream = true
                };
            }

            foreach (KeyValuePair<string, object> systemProperty in SystemProperties)
            {
                // MessageId would already be there.
                if (message.SystemProperties.ContainsKey(systemProperty.Key))
                {
                    message.SystemProperties[systemProperty.Key] = systemProperty.Value;
                }
                else
                {
                    message.SystemProperties.Add(systemProperty);
                }
            }

            foreach (KeyValuePair<string, string> property in Properties)
            {
                message.Properties.Add(property);
            }

            return message;
        }

        /// <summary>
        /// Dispose the current event data instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
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
            ThrowIfDisposed();
            SetGetBodyCalled();
            return _bodyStream ?? Stream.Null;
        }

        /// <summary>
        /// This methods return the body stream as a byte array
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException">throws if the method has been called.</exception>
        /// <exception cref="ObjectDisposedException">throws if the event data has already been disposed.</exception>
        public byte[] GetBytes()
        {
            ThrowIfDisposed();
            SetGetBodyCalled();
            if (_bodyStream == null)
            {
#if NET451
                return new byte[] { };
#else
                return Array.Empty<byte>();
#endif
            }

            if (_bodyStream is BufferListStream listStream)
            {
                // We can trust Amqp bufferListStream.Length;
                var bytes = new byte[listStream.Length];
                listStream.Read(bytes, 0, bytes.Length);
                return bytes;
            }

            // This is just fail safe code in case we are not using the Amqp protocol.
            return ReadFullStream(_bodyStream);
        }

        internal AmqpMessage ToAmqpMessage(bool setBodyCalled = true)
        {
            ThrowIfDisposed();
            if (_serializedAmqpMessage == null)
            {
                try
                {
                    _amqpMessageSemaphore.Wait();
                    if (_serializedAmqpMessage == null)
                    {
                        // Interlocked exchange two variable does allow for a small period
                        // where one is set while the other is not. Not sure if it is worth
                        // correct this gap. The intention of setting this two variable is
                        // so that GetBody should not be called and all Properties are
                        // readonly because the amqpMessage has been serialized.
                        if (setBodyCalled)
                        {
                            SetGetBodyCalled();
                        }

                        SetSizeInBytesCalled();
                        _serializedAmqpMessage = _bodyStream == null
                            ? AmqpMessage.Create()
                            : AmqpMessage.Create(_bodyStream, false);
                        _serializedAmqpMessage = PopulateAmqpMessageForSend(_serializedAmqpMessage);
                    }
                }
                finally
                {
                    _amqpMessageSemaphore.Release();
                }
            }

            return _serializedAmqpMessage;
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

        private void SetGetBodyCalled()
        {
            if (1 == Interlocked.Exchange(ref _getBodyCalled, 1))
            {
                throw Fx.Exception.AsError(new InvalidOperationException(ApiResources.MessageBodyConsumed));
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
            using var ms = new MemoryStream();
            inputStream.CopyTo(ms);
            return ms.ToArray();
        }

        private static Stream CloneStream(Stream originalStream)
        {
            if (originalStream != null)
            {
                if (originalStream is MemoryStream memoryStream)
                {
                    // Note: memoryStream.GetBuffer() doesn't work
                    return new MemoryStream(memoryStream.ToArray(), 0, (int)memoryStream.Length, false, true);
                }

                if (originalStream is ICloneable cloneable)
                {
                    return (Stream)cloneable.Clone();
                }

                if (originalStream.Length == 0)
                {
                    // This can happen in Stream.Null
                    return Stream.Null;
                }

                throw Fx.AssertAndThrow("Does not support cloning of Stream Type: " + originalStream.GetType());
            }
            return null;
        }

        private AmqpMessage PopulateAmqpMessageForSend(AmqpMessage message)
        {
            MessageConverter.UpdateAmqpMessageHeadersAndProperties(message, this);
            return message;
        }

        private T GetSystemProperty<T>(string key)
        {
            return SystemProperties.ContainsKey(key)
                ? (T)SystemProperties[key]
                : default;
        }

        private void ThrowIfDisposed()
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
                    if (_serializedAmqpMessage != null)
                    {
                        // in the receive scenario, this.bodyStream is a reference
                        // to serializedAmqpMessage.BodyStream, and we assume disposing
                        // the amqpMessage will dispose the body stream so we don't
                        // need to dispose bodyStream twice.
                        _serializedAmqpMessage.Dispose();
                        _serializedAmqpMessage = null;
                        _bodyStream = null;
                    }
                    else if (_bodyStream != null && _ownsBodyStream)
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
