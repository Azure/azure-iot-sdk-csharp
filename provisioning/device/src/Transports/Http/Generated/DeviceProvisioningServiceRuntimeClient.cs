// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// API for using the IotHub Device Provisioning Service Runtime Features.
    /// </summary>
    internal partial class DeviceProvisioningServiceRuntimeClient
        : ServiceClient<DeviceProvisioningServiceRuntimeClient>, IDeviceProvisioningServiceRuntimeClient
    {
        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        protected DeviceProvisioningServiceRuntimeClient(params DelegatingHandler[] handlers) : base(handlers)
        {
            Initialize();
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        protected DeviceProvisioningServiceRuntimeClient(
            HttpClientHandler rootHandler,
            params DelegatingHandler[] handlers) : base(rootHandler, handlers)
        {
            Initialize();
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        protected DeviceProvisioningServiceRuntimeClient(Uri baseUri, params DelegatingHandler[] handlers) : this(handlers)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        protected DeviceProvisioningServiceRuntimeClient(
            Uri baseUri,
            HttpClientHandler rootHandler,
            params DelegatingHandler[] handlers) : this(rootHandler, handlers)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        public DeviceProvisioningServiceRuntimeClient(
            ServiceClientCredentials credentials,
            params DelegatingHandler[] handlers) : this(handlers)
        {
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Credentials.InitializeServiceClient(this);
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        public DeviceProvisioningServiceRuntimeClient(
            ServiceClientCredentials credentials,
            HttpClientHandler rootHandler,
            params DelegatingHandler[] handlers) : this(rootHandler, handlers)
        {
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Credentials.InitializeServiceClient(this);
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        public DeviceProvisioningServiceRuntimeClient(
            Uri baseUri,
            ServiceClientCredentials credentials,
            params DelegatingHandler[] handlers) : this(handlers)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Credentials.InitializeServiceClient(this);
        }

        /// <summary>
        /// Creates an instance of this class.
        /// </summary>
        /// <param name='baseUri'>
        /// Optional. The base URI of the service.
        /// </param>
        /// <param name='credentials'>
        /// Required. Subscription credentials which uniquely identify client subscription.
        /// </param>
        /// <param name='rootHandler'>
        /// Optional. The http client handler used to handle http transport.
        /// </param>
        /// <param name='handlers'>
        /// Optional. The delegating handlers to add to the http client pipeline.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        public DeviceProvisioningServiceRuntimeClient(
            Uri baseUri,
            ServiceClientCredentials credentials,
            HttpClientHandler rootHandler,
            params DelegatingHandler[] handlers) : this(rootHandler, handlers)
        {
            BaseUri = baseUri ?? throw new ArgumentNullException(nameof(baseUri));
            Credentials = credentials ?? throw new ArgumentNullException(nameof(credentials));
            Credentials.InitializeServiceClient(this);
        }

        /// <summary>
        /// The base URI of the service.
        /// </summary>
        public Uri BaseUri { get; set; }

        /// <summary>
        /// Gets or sets json serialization settings.
        /// </summary>
        public JsonSerializerSettings SerializationSettings { get; private set; }

        /// <summary>
        /// Gets or sets json deserialization settings.
        /// </summary>
        public JsonSerializerSettings DeserializationSettings { get; private set; }

        /// <summary>
        /// Subscription credentials which uniquely identify client subscription.
        /// </summary>
        public ServiceClientCredentials Credentials { get; private set; }

        /// <summary>
        /// Gets the IRuntimeRegistration.
        /// </summary>
        public virtual IRuntimeRegistration RuntimeRegistration { get; private set; }

        /// <summary>
        /// An optional partial-method to perform custom initialization.
        ///</summary>
        partial void CustomInitialize();

        /// <summary>
        /// Initializes client properties.
        /// </summary>
        private void Initialize()
        {
            RuntimeRegistration = new RuntimeRegistration(this);
            SerializationSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                    {
                        new Iso8601TimeSpanConverter(),
                    }
            };
            DeserializationSettings = new JsonSerializerSettings
            {
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                NullValueHandling = NullValueHandling.Ignore,
                ReferenceLoopHandling = ReferenceLoopHandling.Serialize,
                ContractResolver = new ReadOnlyJsonContractResolver(),
                Converters = new List<JsonConverter>
                    {
                        new Iso8601TimeSpanConverter(),
                    }
            };
            CustomInitialize();
        }
    }
}
