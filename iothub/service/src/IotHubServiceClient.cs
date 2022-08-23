// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net.Http;
using Azure;
using Azure.Core;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The client for making service requests to IoT hub. This client contains subclients for the various feature sets
    /// within IoT hub including managing device/module identities, getting/setting twin for device/modules, invoking
    /// direct methods on devices/modules, and more.
    /// </summary>
    /// <remarks>
    /// This client is <see cref="IDisposable"/> but users are not responsible for disposing subclients within this client.
    /// <para>
    /// This client creates a lifetime long instance of <see cref="HttpClient"/> that is tied to the URI of the
    /// IoT hub specified and configured with any proxy settings provided.
    /// For that reason, the instances are not static and an application using this client
    /// should create and save it for all use. Repeated creation may cause
    /// <see href="https://docs.microsoft.com/azure/architecture/antipatterns/improper-instantiation/">socket exhaustion</see>.
    /// </para>
    /// </remarks>
    public class IotHubServiceClient : IDisposable
    {
        private readonly string _hostName;
        private readonly IotHubConnectionProperties _credentialProvider;
        private readonly HttpClient _httpClient;
        private readonly HttpRequestMessageFactory _httpRequestMessageFactory;
        private const string ApiVersion = "2021-04-12";

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected IotHubServiceClient()
        {
        }

        /// <summary>
        /// Create an instance of this class that authenticates service requests using an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="options">The optional client settings.</param>
        /// <exception cref="ArgumentNullException">Thrown when the provided connection string is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided connection string is empty or whitespace.</exception>
        public IotHubServiceClient(string connectionString, IotHubServiceClientOptions options = default)
        {
            Argument.RequireNotNullOrEmpty(connectionString, nameof(connectionString));

            if (options == null)
            {
                options = new IotHubServiceClientOptions();
            }

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            _credentialProvider = iotHubConnectionString;
            _hostName = iotHubConnectionString.HostName;
            _httpClient = HttpClientFactory.Create(_hostName, options);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(_credentialProvider.HttpsEndpoint, ApiVersion);
            InitializeSubclients(options);
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
        /// <exception cref="ArgumentNullException">Thrown when the provided hostName or credential is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided hostName is empty or whitespace.</exception>
        public IotHubServiceClient(string hostName, TokenCredential credential, IotHubServiceClientOptions options = default)
        {
            Argument.RequireNotNullOrEmpty(hostName, nameof(hostName));
            Argument.RequireNotNull(credential, nameof(credential));

            if (options == null)
            {
                options = new IotHubServiceClientOptions();
            }

            _credentialProvider = new IotHubTokenCrendentialProperties(hostName, credential);
            _hostName = hostName;
            _httpClient = HttpClientFactory.Create(_hostName, options);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(_credentialProvider.HttpsEndpoint, ApiVersion);

            InitializeSubclients(options);
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
        /// <exception cref="ArgumentNullException">Thrown when the provided hostName or credential is null.</exception>
        /// <exception cref="ArgumentException">Thrown when the provided hostName is empty or whitespace.</exception>
        public IotHubServiceClient(string hostName, AzureSasCredential credential, IotHubServiceClientOptions options = default)
        {
            Argument.RequireNotNullOrEmpty(hostName, nameof(hostName));
            Argument.RequireNotNull(credential, nameof(credential));

            if (options == null)
            {
                options = new IotHubServiceClientOptions();
            }

            _credentialProvider = new IotHubSasCredentialProperties(hostName, credential);
            _hostName = hostName;
            _httpClient = HttpClientFactory.Create(_hostName, options);
            _httpRequestMessageFactory = new HttpRequestMessageFactory(_credentialProvider.HttpsEndpoint, ApiVersion);

            InitializeSubclients(options);
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
        public MessageFeedbackProcessorClient MessageFeedbackProcessor { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for receiving file upload notifications.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-file-upload#service-file-upload-notifications"/>.
        public FileUploadNotificationProcessorClient FileUploadNotificationProcessor { get; protected set; }

        /// <summary>
        /// Subclient of <see cref="IotHubServiceClient"/> for sending cloud-to-device and cloud-to-module messages.
        /// </summary>
        /// <seealso href="https://docs.microsoft.com/azure/iot-hub/iot-hub-devguide-messages-c2d"/>.
        public MessagingClient Messaging { get; protected set; }

        /// <summary>
        /// Dispose this client and all the disposable resources it has. This includes any HTTP clients
        /// created by or given to this client.
        /// </summary>
        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        private void InitializeSubclients(IotHubServiceClientOptions _options)
        {
            Devices = new DevicesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            Modules = new ModulesClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            Query = new QueryClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            Configurations = new ConfigurationsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            ScheduledJobs = new ScheduledJobsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, Query);
            DirectMethods = new DirectMethodsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            DigitalTwins = new DigitalTwinsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            Twins = new TwinsClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory);
            Messaging = new MessagingClient(_hostName, _credentialProvider, _httpClient, _httpRequestMessageFactory, _options);

            MessageFeedbackProcessor = new MessageFeedbackProcessorClient(_hostName, _credentialProvider, _options);
            FileUploadNotificationProcessor = new FileUploadNotificationProcessorClient(_hostName, _credentialProvider, _options);

            // Adds additional logging to the AMQP connections created by this client
            AmqpTrace.Provider = new AmqpTransportLog();
        }
    }
}
