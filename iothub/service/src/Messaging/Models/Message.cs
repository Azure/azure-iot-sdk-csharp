// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The data structure represent the message that is used for interacting with IotHub.
    /// </summary>
    public sealed class Message : IDisposable
    {
        private volatile Stream _bodyStream;
        private bool _disposed;
        private StreamDisposalResponsibility _streamDisposalResponsibility;
        private int _getBodyCalled;

        private const long StreamCannotSeek = -1;
        private long _originalStreamPosition = StreamCannotSeek;

        /// <summary>
        /// Default constructor with no body data
        /// </summary>
        public Message()
        {
            Properties = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            SystemProperties = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            InitializeWithStream(Stream.Null, StreamDisposalResponsibility.Sdk);
        }

        /// <summary>
        /// Constructor which uses the argument stream as the body stream.
        /// </summary>
        /// <remarks>User is expected to own the disposing of the stream when using this constructor.</remarks>
        /// <param name="stream">a stream which will be used as body stream.</param>
        public Message(Stream stream)
            : this()
        {
            if (stream != null)
            {
                InitializeWithStream(stream, StreamDisposalResponsibility.App);
            }
        }

        /// <summary>
        /// Constructor which uses the input byte array as the body.
        /// </summary>
        /// <remarks>User should treat the input byte array as immutable when sending the message.</remarks>
        /// <param name="byteArray">A byte array which will be used to form the body stream.</param>
        public Message(byte[] byteArray)
            : this(new MemoryStream(byteArray))
        {
            // Reset the owning of the stream.
            _streamDisposalResponsibility = StreamDisposalResponsibility.Sdk;
        }

        /// <summary>
        /// This constructor is only used in the receive path from AMQP path, or in cloning from a Message that has serialized.
        /// </summary>
        /// <param name="amqpMessage">The AMQP message received, or the message to be cloned.</param>
        internal Message(AmqpMessage amqpMessage)
            : this()
        {
            if (amqpMessage == null)
            {
                throw Fx.Exception.ArgumentNull(nameof(amqpMessage));
            }

            MessageConverter.UpdateMessageHeaderAndProperties(amqpMessage, this);
            Stream stream = amqpMessage.BodyStream;
            InitializeWithStream(stream, StreamDisposalResponsibility.Sdk);
        }

        /// <summary>
        /// This constructor is only used on the Gateway HTTP path so that we can clean up the stream.
        /// </summary>
        /// <param name="stream">A stream which will be used as body stream.</param>
        /// <param name="streamDisposalResponsibility">Indicates if the stream passed in should be disposed by the client library, or by the calling application.</param>
        internal Message(Stream stream, StreamDisposalResponsibility streamDisposalResponsibility)
            : this(stream)
        {
            _streamDisposalResponsibility = streamDisposalResponsibility;
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
            get => GetSystemProperty<string>(MessageSystemPropertyNames.MessageId);
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
        public string CorrelationId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.CorrelationId);
            set => SystemProperties[MessageSystemPropertyNames.CorrelationId] = value;
        }

        /// <summary>
        /// Used in cloud-to-device messages to request IoT hub to generate feedback messages as a result of the consumption of the message by the device.
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
            internal set => SystemProperties[MessageSystemPropertyNames.LockToken] = value;
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

        internal Stream BodyStream => _bodyStream;

        internal bool HasBodyStream()
        {
            return _bodyStream != null;
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

            var message = new Message();
            if (_bodyStream != null)
            {
                // The new Message always owns the cloned stream.
                message = new Message(CloneStream(_bodyStream))
                {
                    _streamDisposalResponsibility = StreamDisposalResponsibility.Sdk
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
                return Array.Empty<byte>();
            }

            if (_bodyStream is BufferListStream listStream)
            {
                // We can trust Amqp bufferListStream.Length;
                byte[] bytes = new byte[listStream.Length];
                listStream.Read(bytes, 0, bytes.Length);
                return bytes;
            }

            // This is just fail safe code in case we are not using the Amqp protocol.
            return ReadFullStream(_bodyStream);
        }

        // The Message body stream needs to be reset if the send operation is to be attempted for the same message.
        internal void ResetBody()
        {
            if (_originalStreamPosition == StreamCannotSeek)
            {
                throw new IOException("Stream cannot seek.");
            }

            if (_bodyStream != null && _bodyStream.CanSeek)
            {
                _bodyStream.Seek(_originalStreamPosition, SeekOrigin.Begin);
            }
            Interlocked.Exchange(ref _getBodyCalled, 0);
        }

        internal bool IsBodyCalled => Volatile.Read(ref _getBodyCalled) == 1;

        private void SetGetBodyCalled()
        {
            if (1 == Interlocked.Exchange(ref _getBodyCalled, 1))
            {
                throw Fx.Exception.AsError(new InvalidOperationException(ApiResources.MessageBodyConsumed));
            }
        }

        private void InitializeWithStream(Stream stream, StreamDisposalResponsibility streamDisposalResponsibility)
        {
            // This method should only be used in constructor because
            // this has no locking on the bodyStream.
            _bodyStream = stream;
            _streamDisposalResponsibility = streamDisposalResponsibility;

            if (_bodyStream.CanSeek)
            {
                _originalStreamPosition = _bodyStream.Position;
            }
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

        private T GetSystemProperty<T>(string key)
        {
            return SystemProperties.ContainsKey(key)
                ? (T)SystemProperties[key]
                : default;
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
                    if (_bodyStream != null && _streamDisposalResponsibility == StreamDisposalResponsibility.Sdk)
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
