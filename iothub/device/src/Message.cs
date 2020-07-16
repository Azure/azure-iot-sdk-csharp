// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Microsoft.Azure.Devices.Client.Extensions;
using Microsoft.Azure.Devices.Client.Common.Api;
using System.Collections.Generic;

namespace Microsoft.Azure.Devices.Client
{
    using DateTimeT = System.DateTime;

    /// <summary>
    /// The data structure represent the message that is used for interacting with IotHub.
    /// </summary>
    public sealed class Message : IReadOnlyIndicator, IDisposable
    {
        private volatile Stream _bodyStream;
        private bool _disposed;
        private bool _ownsBodyStream;

        private const long StreamCannotSeek = -1;
        private long _originalStreamPosition = StreamCannotSeek;

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
        public Message(
            byte[] byteArray)
            : this(new MemoryStream(byteArray))
        {
            // reset the owning of the steams
            _ownsBodyStream = true;
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
        public DateTimeT ExpiryTimeUtc
        {
            get => GetSystemProperty<DateTimeT>(MessageSystemPropertyNames.ExpiryTimeUtc);
            internal set => SystemProperties[MessageSystemPropertyNames.ExpiryTimeUtc] = value;
        }

        /// <summary>
        /// Used in message responses and feedback
        /// </summary>
        public string CorrelationId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.CorrelationId);
            set => SystemProperties[MessageSystemPropertyNames.CorrelationId] = value;
        }

        /// <summary>
        /// Indicates whether consumption or expiration of the message should post data to the feedback queue
        /// </summary>
        private DeliveryAcknowledgement Ack
        {
            get
            {
                string deliveryAckAsString = GetSystemProperty<string>(MessageSystemPropertyNames.Ack);

                return !deliveryAckAsString.IsNullOrWhiteSpace()
                    ? Utils.ConvertDeliveryAckTypeFromString(deliveryAckAsString)
                    : DeliveryAcknowledgement.None;
            }
            set => SystemProperties[MessageSystemPropertyNames.Ack] = Utils.ConvertDeliveryAckTypeToString(value);
        }

        /// <summary>
        /// [Required] SequenceNumber of the received message
        /// </summary>
        public ulong SequenceNumber
        {
            get => GetSystemProperty<ulong>(MessageSystemPropertyNames.SequenceNumber);
            internal set => SystemProperties[MessageSystemPropertyNames.SequenceNumber] = value;
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
        public DateTimeT EnqueuedTimeUtc
        {
            get => GetSystemProperty<DateTimeT>(MessageSystemPropertyNames.EnqueuedTime);
            internal set => SystemProperties[MessageSystemPropertyNames.EnqueuedTime] = value;
        }

        /// <summary>
        /// Number of times the message has been previously delivered
        /// </summary>
        public uint DeliveryCount
        {
            get => GetSystemProperty<byte>(MessageSystemPropertyNames.DeliveryCount);
            internal set => SystemProperties[MessageSystemPropertyNames.DeliveryCount] = (byte)value;
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
        /// For outgoing messages, contains the Mqtt topic that the message is being sent to
        /// For incoming messages, contains the Mqtt topic that the message arrived on
        /// </summary>
        internal string MqttTopicName { get; set; }

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
        public DateTimeT CreationTimeUtc
        {
            get => GetSystemProperty<DateTimeT>(MessageSystemPropertyNames.CreationTimeUtc);
            set => SystemProperties[MessageSystemPropertyNames.CreationTimeUtc] = value;
        }

        /// <summary>
        /// True if the message is set as a security message
        /// </summary>
        public bool IsSecurityMessage => CommonConstants.SecurityMessageInterfaceId.Equals(GetSystemProperty<string>(MessageSystemPropertyNames.InterfaceId), StringComparison.Ordinal);

        /// <summary>
        /// Used to specify the content type of the message.
        /// </summary>
        public string ContentType
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ContentType);
            set => SystemProperties[MessageSystemPropertyNames.ContentType] = value;
        }

        /// <summary>
        /// Specifies the input name on which the message was sent, if there was one.
        /// </summary>
        public string InputName
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.InputName);
            internal set => SystemProperties[MessageSystemPropertyNames.InputName] = value;
        }

        /// <summary>
        /// Specifies the device Id from which this message was sent, if there is one.
        /// </summary>
        public string ConnectionDeviceId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ConnectionDeviceId);
            internal set => SystemProperties[MessageSystemPropertyNames.ConnectionDeviceId] = value;
        }

        /// <summary>
        /// Specifies the module Id from which this message was sent, if there is one.
        /// </summary>
        public string ConnectionModuleId
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ConnectionModuleId);
            internal set => SystemProperties[MessageSystemPropertyNames.ConnectionModuleId] = value;
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
        /// The DTDL component name from where the telemetry message has originated.
        /// This is relevant only for plug and play certified devices.
        /// </summary>
        public string ComponentName
        {
            get => GetSystemProperty<string>(MessageSystemPropertyNames.ComponentName);
            set => SystemProperties[MessageSystemPropertyNames.ComponentName] = value;
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

        public Stream BodyStream => _bodyStream;

        /// <summary>
        /// Gets or sets the deliveryTag which is used for server side checkpointing.
        /// </summary>
        internal ArraySegment<byte> DeliveryTag { get; set; }

        /// <summary>
        /// Dispose the current event data instance
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
        }

        internal bool HasBodyStream()
        {
            return _bodyStream != null;
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

            return ReadFullStream(_bodyStream);
        }

        /// <summary>
        /// Clones an existing <see cref="Message"/> instance and sets content body defined by <paramref name="byteArray"/> on it.
        /// </summary>
        /// <param name="byteArray">Message content to be set after clone.</param>
        /// <returns>A new instance of <see cref="Message"/> with body content defined by <paramref name="byteArray"/>,
        /// and user/system properties of the cloned <see cref="Message"/> instance.
        /// </returns>
        /// <remarks>user should treat the input byte array as immutable when
        /// sending the message.</remarks>
        public Message CloneWithBody(in byte[] byteArray)
        {
            var result = new Message(byteArray);

            foreach (string key in Properties.Keys)
            {
                result.Properties.Add(key, Properties[key]);
            }

            foreach (string key in SystemProperties.Keys)
            {
                result.SystemProperties.Add(key, SystemProperties[key]);
            }

            return result;
        }

        internal void ResetBody()
        {
            if (_originalStreamPosition == StreamCannotSeek)
            {
                throw new IOException("Stream cannot seek.");
            }

            _bodyStream.Seek(_originalStreamPosition, SeekOrigin.Begin);
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

        /// <summary>
        /// Sets the message as an security message
        /// </summary>
        public void SetAsSecurityMessage()
        {
            SystemProperties[MessageSystemPropertyNames.InterfaceId] = CommonConstants.SecurityMessageInterfaceId;
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

            if (_bodyStream.CanSeek)
            {
                _originalStreamPosition = _bodyStream.Position;
            }
        }

        private static byte[] ReadFullStream(Stream inputStream)
        {
            using (var ms = new MemoryStream())
            {
                inputStream.CopyTo(ms);
                return ms.ToArray();
            }
        }

        private T GetSystemProperty<T>(string key)
        {
            if (SystemProperties.ContainsKey(key))
            {
                return (T)SystemProperties[key];
            }

            return default(T);
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
            }

            _disposed = true;
        }
    }
}
