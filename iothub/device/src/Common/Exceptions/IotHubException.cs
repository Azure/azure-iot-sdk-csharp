// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client.Exceptions
{
    using System;
#if !NETMF
    using System.Runtime.Serialization;
#endif

    /// <summary>
    /// The exception that is thrown when an error occurs during DeviceClient or ModuleClient operation.
    /// </summary>
    [Serializable]
    public class IotHubException : Exception
    {
        [NonSerialized]
        private const string IsTransientValueSerializationStoreName = "IotHubException-IsTransient";

        [NonSerialized]
        private const string TrackingIdValueSerializationStoreName = "IotHubException-TrackingId";

        [NonSerialized]
        private bool _isTransient;

        [NonSerialized]
        private string _trackingId;

        /// <summary>
        /// Gets a value indicating if the error is transient.
        /// </summary>
        public bool IsTransient
        {
            get
            {
                return _isTransient;
            }
            private set
            {
                _isTransient = value;
            }
        }

        /// <summary>
        /// Gets or sets the Azure IoT service-side Tracking ID in Support Requests.
        /// </summary>
        public string TrackingId
        {
            get
            {
                return _trackingId;
            }
            set
            {
                _trackingId = value;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        public IotHubException() : base()
        {
        }

        internal IotHubException(bool isTransient) : base()
        {
            this.IsTransient = isTransient;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        public IotHubException(string message)
            : this(message, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="trackingId">The Azure IoT service-side Tracking ID in Support Requests.</param>
        public IotHubException(string message, string trackingId)
            : this(message, false, trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="isTransient">True if the error should be marked as transient.</param>
        /// <param name="trackingId">The Azure IoT service-side Tracking ID in Support Requests.</param>
        public IotHubException(string message, bool isTransient, string trackingId)
            : this(message, null, isTransient, trackingId)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="isTransient">True if the error should be marked as transient.</param>
        public IotHubException(string message, bool isTransient)
            : this(message, null, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception..</param>
        public IotHubException(Exception innerException)
            : base(string.Empty, innerException)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception..</param>
        public IotHubException(string message, Exception innerException)
            : this(message, innerException, false, string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception..</param>
        /// <param name="isTransient">True if the error should be marked as transient.</param>
        protected IotHubException(string message, Exception innerException, bool isTransient)
            : this(message, innerException, isTransient, trackingId: string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="message">The error message.</param>
        /// <param name="innerException">The <see cref="Exception"/> instance that caused the current exception..</param>
        /// <param name="isTransient">True if the error should be marked as transient.</param>
        /// <param name="trackingId">The Azure IoT service-side Tracking ID in Support Requests.</param>
        protected IotHubException(string message, Exception innerException, bool isTransient, string trackingId)
            : base(message, innerException)
        {
            this.IsTransient = isTransient;
            this.TrackingId = trackingId;
        }

#if !NETMF && !NETSTANDARD1_3
        /// <summary>
        /// Initializes a new instance of the <see cref="IotHubException"/> class.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        protected IotHubException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
            if (info != null)
            {
                _isTransient = info.GetBoolean(IsTransientValueSerializationStoreName);
                _trackingId = info.GetString(TrackingIdValueSerializationStoreName);
            }
        }

        /// <summary>
        /// Sets the SerializationInfo object.
        /// </summary>
        /// <param name="info">The serialization info.</param>
        /// <param name="context">The contextual information about the source or destination.</param>
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue(IsTransientValueSerializationStoreName, _isTransient);
            info.AddValue(TrackingIdValueSerializationStoreName, _trackingId);
        }
#endif
    }
}
