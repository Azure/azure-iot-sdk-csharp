// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;
using Microsoft.Azure.Devices.Client.Common.Api;
using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Client
{
    /// <summary>
    /// The data structure represent the message that is used for interacting with IotHub.
    /// </summary>
    public sealed class Message : IReadOnlyIndicator, IDisposable
    {
        private volatile Stream _bodyStream;
        private bool _disposed;
        private StreamDisposalResponsibility _streamDisposalResponsibility;

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
            InitializeWithStream(Stream.Null, StreamDisposalResponsibility.Sdk);
        }

        /// <summary>
        /// Constructor which uses the argument stream as the body stream.
        /// </summary>
        /// <remarks>User is expected to own the disposing of the stream when using this constructor.</remarks>
        /// <param name="stream">A stream which will be used as body stream.</param>
        // UWP cannot expose a method with System.IO.Stream in signature. TODO: consider adding an IRandomAccessStream overload
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
            // Reset the owning of the stream
            _streamDisposalResponsibility = StreamDisposalResponsibility.Sdk;
        }

        /// <summary>
        /// This constructor is only used on the Gateway HTTP path so that we can clean up the stream.
        /// </summary>
        /// <param name="stream">A stream which will be used as body stream.</param>
        /// <param name="streamDisposalResponsibility">Indicates if the stream passed in should be disposed by the
        /// client library, or by the calling application.</param>
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
        /// Date and time when the device-to-cloud message was received by the server.
        /// </summary>
        public DateTime EnqueuedTimeUtc
        {
            get => GetSystemProperty<DateTime>(MessageSystemPropertyNames.EnqueuedTime);
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
        public DateTime CreationTimeUtc
        {
            get => GetSystemProperty<DateTime>(MessageSystemPropertyNames.CreationTimeUtc);
            set => SystemProperties[MessageSystemPropertyNames.CreationTimeUtc] = value;
        }

        /// <summary>
        /// True if the message is set as a security message
        /// </summary>
        public bool IsSecurityMessage => CommonConstants.SecurityMessageInterfaceId.Equals(
            GetSystemProperty<string>(MessageSystemPropertyNames.InterfaceId),
            StringComparison.Ordinal);

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

        /// <summary>
        /// The body stream of the current event data instance
        /// </summary>
        [SuppressMessage(
            "Naming",
            "CA1721:Property names should not match get methods",
            Justification = "Cannot remove public property on a public facing type")]
        public Stream BodyStream => _bodyStream;

        /// <summary>
        /// Gets or sets the deliveryTag which is used for server side check-pointing.
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
            return _bodyStream ?? Stream.Null;
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
        /// <remarks>
        /// The cloned message has the message <see cref="MessageId" /> as the original message.
        /// User should treat the input byte array as immutable when sending the message.
        /// </remarks>
        /// <param name="byteArray">Message content to be set after clone.</param>
        /// <returns>A new instance of <see cref="Message"/> with body content defined by <paramref name="byteArray"/>,
        /// and user/system properties of the cloned <see cref="Message"/> instance.
        /// </returns>
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
            }

            _disposed = true;
        }
    }
}
