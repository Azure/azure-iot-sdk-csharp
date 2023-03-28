﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Amqp;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The client for making service requests to IoT hub.
    /// </summary>
    /// <remarks>
    /// This client contains subclients for the various feature sets within IoT hub including managing device/module
    /// identities, getting/setting twin for device/modules, invoking direct methods on devices/modules, and more.
    /// <para>
    /// This client is <see cref="IDisposable"/>, which will dispose the subclients.
    /// </para>
    /// <para>
    /// This client creates a lifetime-long instance of <see cref="HttpClient"/> that is tied to the URI of the
    /// IoT hub specified and configured with any proxy settings provided.
    /// For that reason, the HttpClient instances are not static and an application using this client
    /// should create and save it for all use. Repeated creation may cause
    /// <see href="https://docs.microsoft.com/azure/architecture/antipatterns/improper-instantiation/">socket exhaustion</see>.
    /// </para>
    /// </remarks>
    public class IotHubServiceClient : IDisposable
    {
        private readonly string _connectionString;
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private readonly IIotHubServiceRetryPolicy _retryPolicy;
        private readonly RetryHandler _retryHandler;
        private readonly IotHubServiceClientOptions _clientOptions;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected IotHubServiceClient()
        {
        }

        private IotHubServiceClient(IotHubServiceClientOptions options)
        {
            _clientOptions = options?.Clone() ?? new();
            _retryPolicy = _clientOptions.RetryPolicy ?? new IotHubServiceNoRetry();
            _retryHandler = new RetryHandler(_retryPolicy);
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests using an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="options">The optional client settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided connection string is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided connection string is empty or whitespace.</exception>
        public IotHubServiceClient(string connectionString, IotHubServiceClientOptions options = default)
            : this(options)
        {
            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));

            _connectionString = connectionString;

            IotHubConnectionString iotHubConnectionString = IotHubConnectionStringParser.Parse(connectionString);
            _credentialProvider = iotHubConnectionString;
            _hostName = iotHubConnectionString.HostName;
            _httpClient = HttpClientFactory.Create(_hostName, _clientOptions);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(
                new UriBuilder(HttpClientFactory.HttpsEndpointPrefix, _hostName).Uri,
                ClientApiVersionHelper.ApiVersionDefault);

            InitializeSubclients();
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests using an identity in Azure Active
        /// Directory (AAD).
        /// </summary>
        /// <remarks>
        /// For more about information on the options of authenticating using a derived instance of <see cref="TokenCredential"/>, see
        /// <see href="https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme"/>.
        /// For more information on configuring IoT hub with Azure Active Directory, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        /// <param name="hostName">IoT hub host name. For instance: "my-iot-hub.azure-devices.net".</param>
        /// <param name="credential">Azure Active Directory (AAD) credentials to authenticate with IoT hub.</param>
        /// <param name="options">The optional client settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="hostName"/> or <paramref name="credential"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided <paramref name="hostName"/> is empty or whitespace.</exception>
        public IotHubServiceClient(string hostName, TokenCredential credential, IotHubServiceClientOptions options = default)
            : this(options)
        {
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));
            Argument.AssertNotNull(credential, nameof(credential));

            _credentialProvider = new IotHubTokenCredentialProperties(hostName, credential);
            _hostName = hostName;
            _httpClient = HttpClientFactory.Create(_hostName, _clientOptions);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(
                new UriBuilder(HttpClientFactory.HttpsEndpointPrefix, _hostName).Uri,
                ClientApiVersionHelper.ApiVersionDefault);

            InitializeSubclients();
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests with a shared access signature
        /// provided and refreshed as necessary by the caller.
        /// </summary>
        /// <remarks>
        /// Users may wish to build their own shared access signature (SAS) tokens rather than give the shared key to the SDK and let it manage signing and renewal.
        /// The <see cref="AzureSasCredential"/> object gives the SDK access to the SAS token, while the caller can update it as necessary using the
        /// <see cref="AzureSasCredential.Update(string)"/> method.
        /// </remarks>
        /// <param name="hostName">IoT hub host name. For instance: "my-iot-hub.azure-devices.net".</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="options">The optional client settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided <paramref name="hostName"/> or <paramref name="credential"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided <paramref name="hostName"/> is empty or whitespace.</exception>
        public IotHubServiceClient(string hostName, AzureSasCredential credential, IotHubServiceClientOptions options = default)
            : this(options)
        {
            Argument.AssertNotNullOrWhiteSpace(hostName, nameof(hostName));
            Argument.AssertNotNull(credential, nameof(credential));

            _credentialProvider = new IotHubSasCredentialProperties(hostName, credential);
            _hostName = hostName;
            _httpClient = HttpClientFactory.Create(_hostName, _clientOptions);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(
                new UriBuilder(HttpClientFactory.HttpsEndpointPrefix, _hostName).Uri,
                ClientApiVersionHelper.ApiVersionDefault);

            InitializeSubclients();
        }

        /// <summary>
        /// The subclient for all device registry operations including getting/adding/setting/deleting
        /// device identities, getting modules on a device, and getting device registry statistics.
        /// </summary>
        public DevicesClient Devices { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> that handles all module registry operations including
        /// getting/adding/setting/deleting module identities.
        /// </summary>
        public ModulesClient Modules { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> that handles configurations
        /// getting/adding/setting/deleting configurations.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-automatic-device-management"/>
        public ConfigurationsClient Configurations { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> to directly invoke direct methods on devices and modules in IoT hub.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/en-us/azure/iot-hub/iot-hub-devguide-direct-methods"/>
        public DirectMethodsClient DirectMethods { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for executing queries using a SQL-like syntax.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-query-language"/>
        public QueryClient Query { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for scheduled jobs management.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-jobs"/>
        public ScheduledJobsClient ScheduledJobs { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> that handles all digital twin operations including
        /// getting a digital twin, updating a digital twin, and invoking commands on a digital twin.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-develop/concepts-digital-twin"/>
        public DigitalTwinsClient DigitalTwins { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> that handles getting, updating, and replacing device and module twins.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-twin-getstarted"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-csharp-csharp-module-twin-getstarted"/>
        public TwinsClient Twins { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for receiving cloud-to-device message feedback.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
        public MessageFeedbackProcessorClient MessageFeedback { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for receiving file upload notifications.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#service-file-upload-notifications"/>.
        public FileUploadNotificationProcessorClient FileUploadNotifications { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for sending cloud-to-device and cloud-to-module messages.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
        public MessagesClient Messages { get; protected set; }
        
        /// <summary>
        /// Requests connection string for the built-in Event Hubs messaging endpoint of the associated IoT hub.
        /// </summary>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>A connection string which can be used to connect to the Event Hubs service and interact with the IoT hub messaging endpoint.</returns>
        /// <exception cref="InvalidOperationException">The Event Hubs host information was not returned by the IoT hub service.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-endpoints"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-read-builtin"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-amqp-support#receive-telemetry-messages-service-client"/>
        public Task<string> GetEventHubCompatibleConnectionStringAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("This overload must only be called if the client was initialized with a connection string.");
            }

            return EventHubConnectionStringBuilder.GetEventHubCompatibleConnectionStringAsync(_connectionString, cancellationToken);
        }

        /// <summary>
        /// Requests connection string for the built-in Event Hubs messaging endpoint of the associated IoT hub.
        /// </summary>
        /// <remarks>
        /// Use this overload when retrieving the Event Hubs compatible connection string for an IotHubServiceClient that was initialized using a method other than IoT hub connection string.
        /// </remarks>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="cancellationToken">The token which allows the operation to be canceled.</param>
        /// <returns>A connection string which can be used to connect to the Event Hubs service and interact with the IoT hub messaging endpoint.</returns>
        /// <exception cref="InvalidOperationException">The Event Hubs host information was not returned by the IoT hub service.</exception>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-endpoints"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-read-builtin"/>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-amqp-support#receive-telemetry-messages-service-client"/>
        [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Client interface should all be instance methods for mocking purposes.")]
        public Task<string> GetEventHubCompatibleConnectionStringAsync(string connectionString, CancellationToken cancellationToken)
        {
            Argument.AssertNotNullOrWhiteSpace(connectionString, nameof(connectionString));
            return EventHubConnectionStringBuilder.GetEventHubCompatibleConnectionStringAsync(connectionString, cancellationToken);
        }

        /// <summary>
        /// Dispose this client and all the disposable resources it has. This includes any HTTP clients
        /// created by or given to this client.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
            GC.SuppressFinalize(this);
        }

        private void InitializeSubclients()
        {
            Devices = new DevicesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            Modules = new ModulesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            Query = new QueryClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            Configurations = new ConfigurationsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            ScheduledJobs = new ScheduledJobsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, Query, _retryHandler);
            DirectMethods = new DirectMethodsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            DigitalTwins = new DigitalTwinsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            Twins = new TwinsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _retryHandler);
            Messages = new MessagesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _clientOptions, _retryHandler);

            MessageFeedback = new MessageFeedbackProcessorClient(_hostName, _credentialProvider, _clientOptions, _retryHandler);
            FileUploadNotifications = new FileUploadNotificationProcessorClient(_hostName, _credentialProvider, _clientOptions, _retryHandler);

            // Specify the JsonSerializerSettings for subclients
            JsonConvert.DefaultSettings = JsonSerializerSettingsInitializer.GetJsonSerializerSettingsDelegate();

            // Adds additional logging to the AMQP connections created by this client
            AmqpTrace.Provider = new AmqpTransportLog();
        }
    }
}
