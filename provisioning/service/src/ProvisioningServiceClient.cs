// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Common.Service.Auth;
using Microsoft.Azure.Devices.Provisioning.Service.Auth;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// Device Provisioning Service Client.
    /// </summary>
    /// <remarks>
    /// The IoT Hub Device Provisioning Service is a helper service for IoT Hub that enables automatic device
    ///     provisioning to a specified IoT hub without requiring human intervention. You can use the Device Provisioning
    ///     Service to provision millions of devices in a secure and scalable manner.
    ///
    /// This C# SDK provides an API to help developers to create and maintain Enrollments on the IoT Hub Device
    ///     Provisioning Service, it translate the rest API in C# Objects and Methods.
    ///
    /// To use the this SDK, you must include the follow package on your application.
    /// <c>
    /// // Include the following using to use the Device Provisioning Service APIs.
    /// using Microsoft.Azure.Devices.Provisioning.Service;
    /// </c>
    ///
    /// The main APIs are exposed by the <see cref="ProvisioningServiceClient"/>, it contains the public Methods that the
    ///     application shall call to create and maintain the Enrollments. The Objects in the <b>configs</b> package shall
    ///     be filled and passed as parameters of the public API, for example, to create a new enrollment, the application
    ///     shall create the object <see cref="IndividualEnrollment"/> with the appropriate enrollment configurations, and call the
    ///     <see cref="CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment)"/>.
    ///
    /// The IoT Hub Device Provisioning Service supports SQL queries too. The application can create a new query using
    ///     one of the queries factories, for instance <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>, passing
    ///     the <see cref="QuerySpecification"/>, with the SQL query. This factory returns a <see cref="Query"/> object, which is an
    ///     active iterator.
    ///
    /// This C# SDK can be represented in the follow diagram, the first layer are the public APIs the your application
    ///     shall use:
    ///
    /// <c>
    /// +===============+       +==========================================+                           +============+   +===+
    /// |    configs    |------>|         ProvisioningServiceClient        |                        +->|    Query   |   |   |
    /// +===============+       +==+=================+==================+==+                        |  +======+=====+   | e |
    ///                           /                  |                   \                          |         |         | x |
    ///                          /                   |                    \                         |         |         | c |
    /// +-----------------------+-----+  +-----------+------------+  +-----+---------------------+  |         |         | e |
    /// | IndividualEnrollmentManager |  | EnrollmentGroupManager |  | RegistrationStatusManager |  |         |         | p |
    /// +---------------+------+------+  +-----------+------+-----+  +-------------+-------+-----+  |         |         | t |
    ///                  \      \                    |       \                     |        \       |         |         | i |
    ///                   \      +----------------------------+------------------------------+------+         |         | o |
    ///                    \                         |                             |                          |         | n |
    ///  +--------+      +--+------------------------+-----------------------------+--------------------------+-----+   | s |
    ///  |  auth  |----->|                                    IContractApiHttp                                      |   |   |
    ///  +--------+      +-------------------------------------------+----------------------------------------------+   +===+
    ///                                                              |
    ///                                                              |
    ///                        +-------------------------------------+------------------------------------------+
    ///                        |                              System.Net.Http                                   |
    ///                        +--------------------------------------------------------------------------------+
    /// </c>
    /// </remarks>
    public class ProvisioningServiceClient : IDisposable
    {
        private readonly ServiceConnectionString _provisioningConnectionString;
        private readonly ProvisioningTokenCredential _provisioningTokenCredential;
        private readonly ProvisioningSasCredential _provisioningSasCredential;
        private readonly IContractApiHttp _contractApiHttp;

        private string _hostName;

        /// <summary>
        /// Create a new instance of the <c>ProvisioningServiceClient</c> that exposes
        /// the API to the Device Provisioning Service.
        /// </summary>
        /// <remarks>
        /// The Device Provisioning Service Client is created based on a <b>Provisioning Connection string</b>.
        /// Once you create a Device Provisioning Service on Azure, you can get the connection string on the Azure portal.
        /// </remarks>
        ///
        /// <param name="connectionString">the <c>string</c> that cares the connection string of the Device Provisioning Service.</param>
        /// <returns>The <c>ProvisioningServiceClient</c> with the new instance of this object.</returns>
        /// <exception cref="ArgumentException">if the connectionString is <c>null</c> or empty.</exception>
        public static ProvisioningServiceClient CreateFromConnectionString(string connectionString)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_001: [The createFromConnectionString shall create a new instance of this class using the provided connectionString.] */
            return new ProvisioningServiceClient(connectionString, new HttpTransportSettings());
        }

        /// <summary>
        /// Create a new instance of the <c>ProvisioningServiceClient</c> that exposes
        /// the API to the Device Provisioning Service.
        /// </summary>
        /// <remarks>
        /// The Device Provisioning Service Client is created based on a <b>Provisioning Connection string</b>.
        /// Once you create a Device Provisioning Service on Azure, you can get the connection string on the Azure portal.
        /// </remarks>
        ///
        /// <param name="connectionString">the <c>string</c> that cares the connection string of the Device Provisioning Service.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings for the request</param>
        /// <returns>The <c>ProvisioningServiceClient</c> with the new instance of this object.</returns>
        /// <exception cref="ArgumentException">if the connectionString is <c>null</c> or empty.</exception>
        public static ProvisioningServiceClient CreateFromConnectionString(string connectionString, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_001: [The createFromConnectionString shall create a new instance of this class using the provided connectionString.] */
            return new ProvisioningServiceClient(connectionString, httpTransportSettings);
        }

        /// <summary>
        /// Create a new instance of the <c>ProvisioningServiceClient</c> from the provided hostName and <c>TokenCredential</c>
        /// that exposes the API to the Device Provisioning Service.
        /// </summary>
        /// 
        /// <param name="hostName">The <c>string</c> that carries the hostName that will be used for this object</param>
        /// <param name="credential">The <c>TokenCredential</c> that provides authentication for this object</param>
        /// <returns>The <c>ProvisioningServiceClient</c> with the new instance of this object.</returns>
        /// <exception cref="ArgumentNullException">if the credential is <c>null</c></exception>
        public static ProvisioningServiceClient Create(string hostName, TokenCredential credential)
        {
            return ProvisioningServiceClient.Create(hostName, credential, new HttpTransportSettings());
        }

        /// <summary>
        /// Create a new instance of the <c>ProvisioningServiceClient</c> from the provided hostName and <c>TokenCredential</c>
        /// that exposes the API to the Device Provisioning Service.
        /// </summary>
        /// 
        /// <param name="hostName">The <c>string</c> that carries the hostName that will be used for this object</param>
        /// <param name="credential">The <c>TokenCredential</c> that provides authentication for this object</param>
        /// <param name="httpTransportSettings">Specifies the HTTP transport settings for the request</param>
        /// <returns>The <c>ProvisioningServiceClient</c> with the new instance of this object.</returns>
        /// <exception cref="ArgumentNullException">if the credential is <c>null</c></exception>
        public static ProvisioningServiceClient Create(string hostName, TokenCredential credential, HttpTransportSettings httpTransportSettings)
        {
            if (credential == null)
            {
                throw new ArgumentNullException(nameof(credential),  "Parameter cannot be null");
            }

            return new ProvisioningServiceClient(hostName, credential, httpTransportSettings);
        }

        /// <summary>
        /// Create a new instance of the <c>ProvisioningServiceClient</c> from the provided hostName and <c>TokenCredential</c>
        /// that exposes the API to the Device Provisioning Service.
        /// </summary>
        /// 
        /// <param name="hostName">The <c>string</c> that carries the host name that will be used for this object</param>
        /// <param name="azureSasCredential">The <c>AzureSasCredential</c> that provides authentication for this object</param>
        /// <returns>The <c>ProvisioningServiceClient</c> with the new instance of this object.</returns>
        /// <exception cref="ArgumentNullException">if the azureSasCredential is <c>null</c></exception>
        public static ProvisioningServiceClient Create(string hostName, AzureSasCredential azureSasCredential)
        {
            return ProvisioningServiceClient.Create(hostName, azureSasCredential, new HttpTransportSettings());
        }

        /// <summary>
        /// Create a new instance of the <c>ProvisioningServiceClient</c> from the provided hostName and <c>TokenCredential</c>
        /// that exposes the API to the Device Provisioning Service.
        /// </summary>
        /// 
        /// <param name="hostName">The <c>string</c> that carries the host name that will be used for this object</param>
        /// <param name="azureSasCredential">The <c>AzureSasCredential</c> that provides authentication for this object</param>
        /// <param name="httpTransportSettings">Specifies the HTTP transport settings for the request</param>
        /// <returns>The <c>ProvisioningServiceClient</c> with the new instance of this object.</returns>
        /// <exception cref="ArgumentNullException">if the azureSasCredential is <c>null</c></exception>
        public static ProvisioningServiceClient Create(string hostName, AzureSasCredential azureSasCredential, HttpTransportSettings httpTransportSettings)
        {
            if (azureSasCredential == null)
            {
                throw new ArgumentNullException(nameof(azureSasCredential),  "Parameter cannot be null");
            }

            return new ProvisioningServiceClient(hostName, azureSasCredential, httpTransportSettings);
        }


        /// <summary>
        /// PRIVATE CONSTRUCTOR
        /// </summary>
        /// <param name="connectionString">the <c>string</c> that contains the connection string for the Provisioning service.</param>
        /// <param name="httpTransportSettings">Specifies the HTTP transport settings for the request</param>
        /// <exception cref="ArgumentException">if the connectionString is <c>null</c>, empty, or invalid.</exception>
        private ProvisioningServiceClient(string connectionString, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_002: [The constructor shall throw ArgumentException if the provided connectionString is null or empty.] */
            if (string.IsNullOrWhiteSpace(connectionString ?? throw new ArgumentNullException(nameof(connectionString))))
            {
                throw new ArgumentException("Connection string cannot be empty", nameof(connectionString));
            }

            /* SRS_PROVISIONING_SERVICE_CLIENT_21_003: [The constructor shall throw ArgumentException if the ProvisioningConnectionString or one of the inner Managers failed to create a new instance.] */
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_004: [The constructor shall create a new instance of the ContractApiHttp class using the provided connectionString.] */
            _provisioningConnectionString = ServiceConnectionString.Parse(connectionString);
            _hostName = _provisioningConnectionString.HostName;
            _contractApiHttp = new ContractApiHttp(
                _provisioningConnectionString.HttpsEndpoint,
                _provisioningConnectionString,
                httpTransportSettings);
        }

        private ProvisioningServiceClient(string hostName, TokenCredential credential, HttpTransportSettings httpTransportSettings)
        {
            if (string.IsNullOrWhiteSpace(hostName ?? throw new ArgumentNullException(nameof(hostName))))
            {
                throw new ArgumentException("Host name cannot be empty", nameof(hostName));
            }

            _provisioningTokenCredential = new ProvisioningTokenCredential(credential);
            _hostName = hostName;
            
            // Create new ContractApiHttp using provided TokenCredential and hostname instead of standard connection string
            _contractApiHttp = new ContractApiHttp(
                new UriBuilder("https", _hostName).Uri,
                _provisioningTokenCredential,
                httpTransportSettings);
        }

        private ProvisioningServiceClient(string hostName, AzureSasCredential azureSasCredential, HttpTransportSettings httpTransportSettings)
        {
            if (string.IsNullOrWhiteSpace(hostName ?? throw new ArgumentNullException(nameof(hostName))))
            {
                throw new ArgumentException("Host name cannot be empty", nameof(hostName));
            }

            _provisioningSasCredential = new ProvisioningSasCredential(azureSasCredential);
            _hostName = hostName;

            // Create new ContractApiHttp using provided AzureSasCredential and hostname instead of standard connection string
            _contractApiHttp = new ContractApiHttp(
                new UriBuilder("https", _hostName).Uri,
                _provisioningSasCredential,
                httpTransportSettings);
        }

        /// <summary>
        /// Dispose the Provisioning Service Client and its dependencies.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by the Component and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_contractApiHttp != null)
                {
                    _contractApiHttp.Dispose();
                }
            }
        }

        /// <summary>
        /// Create or update a individual Device Enrollment record.
        /// </summary>
        /// <remarks>
        /// This API creates a new individualEnrollment or update a existed one. All enrollments in the Device Provisioning Service
        ///     contains a unique identifier called registrationId. If this API is called for an individualEnrollment with a
        ///     registrationId that already exists, it will replace the existed individualEnrollment information by the new one.
        ///     On the other hand, if the registrationId does not exit, this API will create a new individualEnrollment.
        ///
        /// If the registrationId already exists, this method will update existed enrollments. Note that update the
        ///     individualEnrollment will not change the status of the device that was already registered using the old individualEnrollment.
        ///
        /// To use the Device Provisioning Service API, you must include the follow package on your application.
        /// <c>
        /// // Include the following using to use the Device Provisioning Service APIs.
        /// using Microsoft.Azure.Devices.Provisioning.Service;
        /// </c>
        /// </remarks>
        /// <example>
        /// The follow code will create a new individualEnrollment that will provisioning the registrationid-1 using TPM attestation.
        /// <c>
        /// // IndividualEnrollment information.
        /// private const string PROVISIONING_CONNECTION_STRING = "HostName=ContosoProvisioning.azure-devices-provisioning.net;" +
        ///                                                       "SharedAccessKeyName=contosoprovisioningserviceowner;" +
        ///                                                       "SharedAccessKey=dGVzdFN0cmluZzE=";
        /// private const string TPM_ENDORSEMENT_KEY = "tpm-endorsement-key";
        /// private const string REGISTRATION_ID = "registrationid-1";
        ///
        /// static void Main(string[] args)
        /// {
        ///     RunSample().GetAwaiter().GetResult();
        /// }
        ///
        /// public static async Task RunSample()
        /// {
        ///     using(ProvisioningServiceClient provisioningServiceClient =
        ///             ProvisioningServiceClient.CreateFromConnectionString(PROVISIONING_CONNECTION_STRING))
        ///     {
        ///         // ************************************ Create the individualEnrollment ****************************************
        ///         Console.WriteLine("\nCreate a new individualEnrollment...");
        ///         Attestation attestation = new TpmAttestation(TPM_ENDORSEMENT_KEY);
        ///         IndividualEnrollment individualEnrollment =
        ///             new IndividualEnrollment(
        ///                 REGISTRATION_ID,
        ///                 attestation);
        ///         individualEnrollment.ProvisioningStatus = ProvisioningStatus.Disabled;
        ///         IndividualEnrollment individualEnrollmentResult =
        ///             await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
        ///         Console.WriteLine("\nIndividualEnrollment created with success...");
        ///     }
        /// }
        /// </c>
        ///
        /// The follow code will update the provisioningStatus of the previous individualEnrollment from <b>disabled</b> to <b>enabled</b>.
        /// <c>
        /// // IndividualEnrollment information.
        /// private const string PROVISIONING_CONNECTION_STRING = "HostName=ContosoProvisioning.azure-devices-provisioning.net;" +
        ///                                                              "SharedAccessKeyName=contosoprovisioningserviceowner;" +
        ///                                                              "SharedAccessKey=dGVzdFN0cmluZzE=";
        /// private const string REGISTRATION_ID = "registrationid-1";
        ///
        /// static void Main(string[] args)
        /// {
        ///     RunSample().GetAwaiter().GetResult();
        /// }
        ///
        /// public static async Task RunSample()
        /// {
        ///     using(ProvisioningServiceClient provisioningServiceClient =
        ///             ProvisioningServiceClient.CreateFromConnectionString(PROVISIONING_CONNECTION_STRING))
        ///     {
        ///         // ************************* Get the content of the previous individualEnrollment ******************************
        ///         Console.WriteLine("\nGet the content of the previous individualEnrollment...");
        ///         Attestation attestation = new TpmAttestation(TPM_ENDORSEMENT_KEY);
        ///         IndividualEnrollment individualEnrollment =
        ///             await deviceProvisioningServiceClient.GetIndividualEnrollmentAsync(REGISTRATION_ID).ConfigureAwait(false);
        ///         individualEnrollment.ProvisioningStatus = ProvisioningStatus.Enabled;
        ///         IndividualEnrollment individualEnrollmentResult =
        ///             await provisioningServiceClient.CreateOrUpdateIndividualEnrollmentAsync(individualEnrollment).ConfigureAwait(false);
        ///         Console.WriteLine("\nIndividualEnrollment updated with success...");
        ///     }
        /// }
        /// </c>
        /// </example>
        ///
        /// <param name="individualEnrollment">the <see cref="IndividualEnrollment"/> object that describes the individualEnrollment that will be created of updated. It cannot be <c>null</c>.</param>
        /// <returns>An <see cref="IndividualEnrollment"/> object with the result of the create or update requested.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to create or update the individualEnrollment.</exception>
        public Task<IndividualEnrollment> CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment individualEnrollment)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_008: [The CreateOrUpdateIndividualEnrollmentAsync shall create a new Provisioning individualEnrollment by calling the CreateOrUpdateAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateOrUpdateAsync(_contractApiHttp, individualEnrollment, CancellationToken.None);
        }

        /// <summary>
        /// Creates or updates an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment object.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<IndividualEnrollment> CreateOrUpdateIndividualEnrollmentAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_008: [The CreateOrUpdateIndividualEnrollmentAsync shall create a new Provisioning individualEnrollment by calling the CreateOrUpdateAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateOrUpdateAsync(_contractApiHttp, individualEnrollment, cancellationToken);
        }

        /// <summary>
        /// Create, update or delete a set of individual Device Enrollments.
        /// </summary>
        /// <remarks>
        /// This API provide the means to do a single operation over multiple individualEnrollments. A valid operation
        ///     is determined by <see cref="BulkOperationMode"/>, and can be 'create', 'update', 'updateIfMatchETag', or 'delete'.
        /// </remarks>
        ///
        /// <param name="bulkOperationMode">the <see cref="BulkOperationMode"/> that defines the single operation to do over the individualEnrollments. It cannot be <c>null</c>.</param>
        /// <param name="individualEnrollments">the collection of <see cref="IndividualEnrollment"/> that contains the description of each individualEnrollment. It cannot be <c>null</c> or empty.</param>
        /// <returns>A <see cref="BulkEnrollmentOperationResult"/> object with the result of operation for each enrollment.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task<BulkEnrollmentOperationResult> RunBulkEnrollmentOperationAsync(
                BulkOperationMode bulkOperationMode, IEnumerable<IndividualEnrollment> individualEnrollments)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_009: [The RunBulkEnrollmentOperationAsync shall do a Provisioning operation over individualEnrollment by calling the BulkOperationAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.BulkOperationAsync(_contractApiHttp, bulkOperationMode, individualEnrollments, CancellationToken.None);
        }

        /// <summary>
        /// Create, update or delete a set of individual Device Enrollments.
        /// </summary>
        /// <remarks>
        /// This API provide the means to do a single operation over multiple individualEnrollments. A valid operation
        ///     is determined by <see cref="BulkOperationMode"/>, and can be 'create', 'update', 'updateIfMatchETag', or 'delete'.
        /// </remarks>
        ///
        /// <param name="bulkOperationMode">the <see cref="BulkOperationMode"/> that defines the single operation to do over the individualEnrollments. It cannot be <c>null</c>.</param>
        /// <param name="individualEnrollments">the collection of <see cref="IndividualEnrollment"/> that contains the description of each individualEnrollment. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="BulkEnrollmentOperationResult"/> object with the result of operation for each enrollment.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task<BulkEnrollmentOperationResult> RunBulkEnrollmentOperationAsync(
                BulkOperationMode bulkOperationMode, IEnumerable<IndividualEnrollment> individualEnrollments, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_009: [The RunBulkEnrollmentOperationAsync shall do a Provisioning operation over individualEnrollment by calling the BulkOperationAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.BulkOperationAsync(_contractApiHttp, bulkOperationMode, individualEnrollments, cancellationToken);
        }

        /// <summary>
        /// Retrieve the individualEnrollment information.
        /// </summary>
        /// <remarks>
        /// This method will return the enrollment information for the provided registrationId. It will retrieve
        ///     the correspondent individualEnrollment from the Device Provisioning Service, and return it in the
        ///     <see cref="IndividualEnrollment"/> object.
        ///
        /// If the registrationId do not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="registrationId">the <c>string</c> that identifies the individualEnrollment. It cannot be <c>null</c> or empty.</param>
        /// <returns>The <see cref="IndividualEnrollment"/> with the content of the individualEnrollment in the Provisioning Device Service.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task<IndividualEnrollment> GetIndividualEnrollmentAsync(string registrationId)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_010: [The GetIndividualEnrollmentAsync shall retrieve the individualEnrollment information for the provided registrationId by calling the GetAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.GetAsync(_contractApiHttp, registrationId, CancellationToken.None);
        }

        /// <summary>
        /// Gets the individual enrollment object.
        /// </summary>
        /// <param name="registrationId">The registration Id.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task<IndividualEnrollment> GetIndividualEnrollmentAsync(string registrationId, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_010: [The GetIndividualEnrollmentAsync shall retrieve the individualEnrollment information for the provided registrationId by calling the GetAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.GetAsync(_contractApiHttp, registrationId, cancellationToken);
        }

        /// <summary>
        /// Delete the individualEnrollment information.
        /// </summary>
        /// <remarks>
        /// This method will remove the individualEnrollment from the Device Provisioning Service using the
        ///     provided <see cref="IndividualEnrollment"/> information. The Device Provisioning Service will care about the
        ///     registrationId and the eTag on the individualEnrollment. If you want to delete the individualEnrollment regardless the
        ///     eTag, you can set the <c>eTag="*"} into the individualEnrollment, or use the {@link #deleteIndividualEnrollment(string)</c>
        ///     passing only the registrationId.
        ///
        /// Note that delete the individualEnrollment will not remove the Device itself from the IotHub.
        ///
        /// If the registrationId does not exists or the eTag not matches, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="individualEnrollment">the <see cref="IndividualEnrollment"/> that identifies the individualEnrollment. It cannot be <c>null</c>.</param>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task DeleteIndividualEnrollmentAsync(IndividualEnrollment individualEnrollment)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_011: [The DeleteIndividualEnrollmentAsync shall delete the individualEnrollment for the provided individualEnrollment by calling the DeleteAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, individualEnrollment, CancellationToken.None);
        }

        /// <summary>
        /// Deletes an individual enrollment.
        /// </summary>
        /// <param name="individualEnrollment">The individual enrollment.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task DeleteIndividualEnrollmentAsync(IndividualEnrollment individualEnrollment, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_011: [The DeleteIndividualEnrollmentAsync shall delete the individualEnrollment for the provided individualEnrollment by calling the DeleteAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, individualEnrollment, cancellationToken);
        }

        /// <summary>
        /// Delete the individualEnrollment information.
        /// </summary>
        /// <remarks>
        /// This method will remove the individualEnrollment from the Device Provisioning Service using the
        ///     provided registrationId. It will delete the enrollment regardless the eTag. It means that this API
        ///     correspond to the <see cref="DeleteIndividualEnrollmentAsync(string, string)"/> with the <c>eTag="*"</c>.
        ///
        /// Note that delete the enrollment will not remove the Device itself from the IotHub.
        ///
        /// If the registrationId does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="registrationId">the <c>string} that identifies the individualEnrollment. It cannot be {@code null</c> or empty.</param>
        /// <exception cref="ArgumentException">if the provided registrationId is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task DeleteIndividualEnrollmentAsync(string registrationId)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_012: [The DeleteIndividualEnrollmentAsync shall delete the individualEnrollment for the provided registrationId by calling the DeleteAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, registrationId, CancellationToken.None);
        }

        /// <summary>
        /// Delete the individualEnrollment information.
        /// </summary>
        /// <remarks>
        /// This method will remove the individualEnrollment from the Device Provisioning Service using the
        ///     provided registrationId. It will delete the enrollment regardless the eTag. It means that this API
        ///     correspond to the <see cref="DeleteIndividualEnrollmentAsync(string, string)"/> with the <c>eTag="*"</c>.
        ///
        /// Note that delete the enrollment will not remove the Device itself from the IotHub.
        ///
        /// If the registrationId does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="registrationId">the <c>string} that identifies the individualEnrollment. It cannot be {@code null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ArgumentException">if the provided registrationId is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task DeleteIndividualEnrollmentAsync(string registrationId, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_012: [The DeleteIndividualEnrollmentAsync shall delete the individualEnrollment for the provided registrationId by calling the DeleteAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, registrationId, cancellationToken);
        }

        /// <summary>
        /// Delete the individualEnrollment information.
        /// </summary>
        /// <remarks>
        /// This method will remove the individualEnrollment from the Device Provisioning Service using the
        ///     provided registrationId and eTag. If you want to delete the enrollment regardless the eTag, you can
        ///     use <see cref="DeleteIndividualEnrollmentAsync(string)"/> or you can pass the eTag as <c>null</c>, empty, or
        ///     <c>"*"</c>.
        ///
        /// Note that delete the enrollment will not remove the Device itself from the IotHub.
        ///
        /// If the registrationId does not exists or the eTag does not matches, this method will throw
        ///     <see cref="ProvisioningServiceClientException"/>. For more exceptions that this method can throw, please see
        /// </remarks>
        /// <param name="registrationId">the <c>string</c> that identifies the individualEnrollment. It cannot be <c>null</c> or empty.</param>
        /// <param name="eTag">the <c>string</c> with the IndividualEnrollment eTag. It can be <c>null</c> or empty.
        ///             The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <exception cref="ArgumentException">if the provided registrationId is not correct.</exception>
        /// <exception cref="ProvisioningServiceClientTransportException">if the SDK failed to send the request to the Device Provisioning Service.</exception>
        /// <exception cref="ProvisioningServiceClientException">if the Device Provisioning Service was not able to execute the bulk operation.</exception>
        public Task DeleteIndividualEnrollmentAsync(string registrationId, string eTag)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_013: [The DeleteIndividualEnrollmentAsync shall delete the individualEnrollment for the provided registrationId and etag by calling the DeleteAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, registrationId, CancellationToken.None, eTag);
        }

        /// <summary>
        /// Deletes an individual enrollment if the eTag matches.
        /// </summary>
        /// <param name="registrationId">The registration id</param>
        /// <param name="eTag">The eTag.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns></returns>
        public Task DeleteIndividualEnrollmentAsync(string registrationId, string eTag, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_013: [The DeleteIndividualEnrollmentAsync shall delete the individualEnrollment for the provided registrationId and etag by calling the DeleteAsync in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.DeleteAsync(_contractApiHttp, registrationId, cancellationToken, eTag);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        ///     as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        public Query CreateIndividualEnrollmentQuery(QuerySpecification querySpecification)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_014: [The CreateIndividualEnrollmentQuery shall create a new individual enrollment query by calling the CreateQuery in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                CancellationToken.None);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        ///     as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        public Query CreateIndividualEnrollmentQuery(QuerySpecification querySpecification, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_014: [The CreateIndividualEnrollmentQuery shall create a new individual enrollment query by calling the CreateQuery in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                httpTransportSettings,
                CancellationToken.None);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        ///     as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        public Query CreateIndividualEnrollmentQuery(QuerySpecification querySpecification, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_014: [The CreateIndividualEnrollmentQuery shall create a new individual enrollment query by calling the CreateQuery in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                cancellationToken);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        ///     as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateIndividualEnrollmentQuery(QuerySpecification querySpecification, int pageSize)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_015: [The CreateIndividualEnrollmentQuery shall create a new individual enrollment query by calling the CreateQuery in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                CancellationToken.None,
                pageSize);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        ///     as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateIndividualEnrollmentQuery(QuerySpecification querySpecification, int pageSize, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_015: [The CreateIndividualEnrollmentQuery shall create a new individual enrollment query by calling the CreateQuery in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                cancellationToken,
                pageSize);
        }

        /// <summary>
        /// Factory to create a individualEnrollment query.
        /// </summary>
        /// <remarks>
        /// This method will create a new individualEnrollment query for Device Provisioning Service and return it
        ///     as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateIndividualEnrollmentQuery(QuerySpecification querySpecification, int pageSize, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_015: [The CreateIndividualEnrollmentQuery shall create a new individual enrollment query by calling the CreateQuery in the IndividualEnrollmentManager.] */
            return IndividualEnrollmentManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                httpTransportSettings,
                CancellationToken.None,
                pageSize);
        }

        /// <summary>
        /// Create or update an enrollment group record.
        /// </summary>
        /// <remarks>
        /// This API creates a new enrollment group or update a existed one. All enrollment group in the Device
        ///     Provisioning Service contains a unique identifier called enrollmentGroupId. If this API is called
        ///     with an enrollmentGroupId that already exists, it will replace the existed enrollmentGroup information
        ///     by the new one. On the other hand, if the enrollmentGroupId does not exit, it will be created.
        ///
        /// To use the Device Provisioning Service API, you must include the follow package on your application.
        /// <c>
        /// // Include the following using to use the Device Provisioning Service APIs.
        /// using Microsoft.Azure.Devices.Provisioning.Service;
        /// </c>
        /// </remarks>
        ///
        /// <param name="enrollmentGroup">the <see cref="EnrollmentGroup"/> object that describes the individualEnrollment that will be created of updated.</param>
        /// <returns>An <see cref="EnrollmentGroup"/> object with the result of the create or update requested.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning was not able to create or update the enrollment.</exception>
        public Task<EnrollmentGroup> CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_016: [The createOrUpdateEnrollmentGroup shall create a new Provisioning enrollmentGroup by calling the createOrUpdate in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateOrUpdateAsync(_contractApiHttp, enrollmentGroup, CancellationToken.None);
        }

        /// <summary>
        /// Create or update an enrollment group record.
        /// </summary>
        /// <remarks>
        /// This API creates a new enrollment group or update a existed one. All enrollment group in the Device
        ///     Provisioning Service contains a unique identifier called enrollmentGroupId. If this API is called
        ///     with an enrollmentGroupId that already exists, it will replace the existed enrollmentGroup information
        ///     by the new one. On the other hand, if the enrollmentGroupId does not exit, it will be created.
        ///
        /// To use the Device Provisioning Service API, you must include the follow package on your application.
        /// <c>
        /// // Include the following using to use the Device Provisioning Service APIs.
        /// using Microsoft.Azure.Devices.Provisioning.Service;
        /// </c>
        /// </remarks>
        ///
        /// <param name="enrollmentGroup">the <see cref="EnrollmentGroup"/> object that describes the individualEnrollment that will be created of updated.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>An <see cref="EnrollmentGroup"/> object with the result of the create or update requested.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning was not able to create or update the enrollment.</exception>
        public Task<EnrollmentGroup> CreateOrUpdateEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_016: [The createOrUpdateEnrollmentGroup shall create a new Provisioning enrollmentGroup by calling the createOrUpdate in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateOrUpdateAsync(_contractApiHttp, enrollmentGroup, cancellationToken);
        }

        /// <summary>
        /// Retrieve the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will return the enrollmentGroup information for the provided enrollmentGroupId. It will retrieve
        ///     the correspondent enrollmentGroup from the Device Provisioning Service, and return it in the
        ///     <see cref="EnrollmentGroup"/> object.
        ///
        /// If the enrollmentGroupId does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <returns>The <see cref="EnrollmentGroup"/> with the content of the enrollmentGroup in the Provisioning Device Service.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to retrieve the enrollmentGroup information for the provided enrollmentGroupId.</exception>
        public Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_017: [The getEnrollmentGroup shall retrieve the enrollmentGroup information for the provided enrollmentGroupId by calling the get in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.GetAsync(_contractApiHttp, enrollmentGroupId, CancellationToken.None);
        }

        /// <summary>
        /// Retrieve the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will return the enrollmentGroup information for the provided enrollmentGroupId. It will retrieve
        ///     the correspondent enrollmentGroup from the Device Provisioning Service, and return it in the
        ///     <see cref="EnrollmentGroup"/> object.
        ///
        /// If the enrollmentGroupId does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="EnrollmentGroup"/> with the content of the enrollmentGroup in the Provisioning Device Service.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to retrieve the enrollmentGroup information for the provided enrollmentGroupId.</exception>
        public Task<EnrollmentGroup> GetEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_017: [The getEnrollmentGroup shall retrieve the enrollmentGroup information for the provided enrollmentGroupId by calling the get in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.GetAsync(_contractApiHttp, enrollmentGroupId, cancellationToken);
        }

        /// <summary>
        /// Delete the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollmentGroup from the Device Provisioning Service using the
        ///     provided <see cref="EnrollmentGroup"/> information. The Device Provisioning Service will care about the
        ///     enrollmentGroupId and the eTag on the enrollmentGroup. If you want to delete the enrollment regardless the
        ///     eTag, you can set the <c>eTag="*"</c> into the enrollmentGroup, or use the <see cref="DeleteEnrollmentGroupAsync(string)"/>.
        ///     passing only the enrollmentGroupId.
        ///
        /// Note that delete the enrollmentGroup will not remove the Devices itself from the IotHub.
        ///
        /// If the enrollmentGroupId does not exists or the eTag does not matches, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroup">the <see cref="EnrollmentGroup"/> that identifies the enrollmentGroup. It cannot be <c>null</c>.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the enrollmentGroup information for the provided enrollmentGroup.</exception>
        public Task DeleteEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_018: [The deleteEnrollmentGroup shall delete the enrollmentGroup for the provided enrollmentGroup by calling the delete in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroup, CancellationToken.None);
        }

        /// <summary>
        /// Delete the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollmentGroup from the Device Provisioning Service using the
        ///     provided <see cref="EnrollmentGroup"/> information. The Device Provisioning Service will care about the
        ///     enrollmentGroupId and the eTag on the enrollmentGroup. If you want to delete the enrollment regardless the
        ///     eTag, you can set the <c>eTag="*"</c> into the enrollmentGroup, or use the <see cref="DeleteEnrollmentGroupAsync(string)"/>.
        ///     passing only the enrollmentGroupId.
        ///
        /// Note that delete the enrollmentGroup will not remove the Devices itself from the IotHub.
        ///
        /// If the enrollmentGroupId does not exists or the eTag does not matches, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroup">the <see cref="EnrollmentGroup"/> that identifies the enrollmentGroup. It cannot be <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the enrollmentGroup information for the provided enrollmentGroup.</exception>
        public Task DeleteEnrollmentGroupAsync(EnrollmentGroup enrollmentGroup, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_018: [The deleteEnrollmentGroup shall delete the enrollmentGroup for the provided enrollmentGroup by calling the delete in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroup, cancellationToken);
        }

        /// <summary>
        /// Delete the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollmentGroup from the Device Provisioning Service using the
        ///     provided enrollmentGroupId. It will delete the enrollmentGroup regardless the eTag. It means that this API
        ///     correspond to the <see cref="DeleteEnrollmentGroupAsync(string, string)"/> with the <c>eTag="*"</c>.
        ///
        /// Note that delete the enrollmentGroup will not remove the Devices itself from the IotHub.
        ///
        /// If the enrollmentGroupId does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the enrollmentGroup information for the provided enrollmentGroupId.</exception>
        public Task DeleteEnrollmentGroupAsync(string enrollmentGroupId)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_019: [The deleteEnrollmentGroup shall delete the enrollmentGroup for the provided enrollmentGroupId by calling the delete in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroupId, CancellationToken.None);
        }

        /// <summary>
        /// Delete the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollmentGroup from the Device Provisioning Service using the
        ///     provided enrollmentGroupId. It will delete the enrollmentGroup regardless the eTag. It means that this API
        ///     correspond to the <see cref="DeleteEnrollmentGroupAsync(string, string)"/> with the <c>eTag="*"</c>.
        ///
        /// Note that delete the enrollmentGroup will not remove the Devices itself from the IotHub.
        ///
        /// If the enrollmentGroupId does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the enrollmentGroup information for the provided enrollmentGroupId.</exception>
        public Task DeleteEnrollmentGroupAsync(string enrollmentGroupId, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_019: [The deleteEnrollmentGroup shall delete the enrollmentGroup for the provided enrollmentGroupId by calling the delete in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroupId, cancellationToken);
        }

        /// <summary>
        /// Delete the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollmentGroup from the Device Provisioning Service using the
        ///     provided enrollmentGroupId and eTag. If you want to delete the enrollmentGroup regardless the eTag, you can
        ///     use <see cref="DeleteEnrollmentGroupAsync(string)"/> or you can pass the eTag as <c>null</c>, empty, or
        ///     <c>"*"</c>.
        ///
        /// Note that delete the enrollmentGroup will not remove the Device itself from the IotHub.
        ///
        /// If the enrollmentGroupId does not exists or eTag does not matches, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="eTag">the <c>string</c> with the enrollmentGroup eTag. It can be <c>null</c> or empty.
        ///             The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the enrollmentGroup information for the provided enrollmentGroupId and eTag.</exception>
        public Task DeleteEnrollmentGroupAsync(string enrollmentGroupId, string eTag)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_020: [The deleteEnrollmentGroup shall delete the enrollmentGroup for the provided enrollmentGroupId and eTag by calling the delete in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroupId, CancellationToken.None, eTag);
        }

        /// <summary>
        /// Delete the enrollmentGroup information.
        /// </summary>
        /// <remarks>
        /// This method will remove the enrollmentGroup from the Device Provisioning Service using the
        ///     provided enrollmentGroupId and eTag. If you want to delete the enrollmentGroup regardless the eTag, you can
        ///     use <see cref="DeleteEnrollmentGroupAsync(string)"/> or you can pass the eTag as <c>null</c>, empty, or
        ///     <c>"*"</c>.
        ///
        /// Note that delete the enrollmentGroup will not remove the Device itself from the IotHub.
        ///
        /// If the enrollmentGroupId does not exists or eTag does not matches, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="eTag">the <c>string</c> with the enrollmentGroup eTag. It can be <c>null</c> or empty.
        ///             The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the enrollmentGroup information for the provided enrollmentGroupId and eTag.</exception>
        public Task DeleteEnrollmentGroupAsync(string enrollmentGroupId, string eTag, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_020: [The deleteEnrollmentGroup shall delete the enrollmentGroup for the provided enrollmentGroupId and eTag by calling the delete in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.DeleteAsync(_contractApiHttp, enrollmentGroupId, cancellationToken, eTag);
        }

        /// <summary>
        /// Factory to create an enrollmentGroup query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        ///     a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        public Query CreateEnrollmentGroupQuery(QuerySpecification querySpecification)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_021: [The createEnrollmentGroupQuery shall create a new enrolmentGroup query by calling the createQuery in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                CancellationToken.None);
        }

        /// <summary>
        /// Factory to create an enrollmentGroup query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        ///     a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        public Query CreateEnrollmentGroupQuery(QuerySpecification querySpecification, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_021: [The createEnrollmentGroupQuery shall create a new enrolmentGroup query by calling the createQuery in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                httpTransportSettings,
                CancellationToken.None);
        }

        /// <summary>
        /// Factory to create an enrollmentGroup query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        ///     a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameter is not correct.</exception>
        public Query CreateEnrollmentGroupQuery(QuerySpecification querySpecification, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_021: [The createEnrollmentGroupQuery shall create a new enrolmentGroup query by calling the createQuery in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                cancellationToken);
        }

        /// <summary>
        /// Factory to create an enrollmentGroup query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        ///     a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateEnrollmentGroupQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateEnrollmentGroupQuery(QuerySpecification querySpecification, int pageSize)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_022: [The createEnrollmentGroupQuery shall create a new enrolmentGroup query by calling the createQuery in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                CancellationToken.None,
                pageSize);
        }

        /// <summary>
        /// Factory to create an enrollmentGroup query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        ///     a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateEnrollmentGroupQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateEnrollmentGroupQuery(QuerySpecification querySpecification, int pageSize, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_022: [The createEnrollmentGroupQuery shall create a new enrolmentGroup query by calling the createQuery in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                cancellationToken,
                pageSize);
        }

        /// <summary>
        /// Factory to create an enrollmentGroup query.
        /// </summary>
        /// <remarks>
        /// This method will create a new enrollment group query on Device Provisioning Service and return it as
        ///     a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateEnrollmentGroupQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateEnrollmentGroupQuery(QuerySpecification querySpecification, int pageSize, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_022: [The createEnrollmentGroupQuery shall create a new enrolmentGroup query by calling the createQuery in the EnrollmentGroupManager.] */
            return EnrollmentGroupManager.CreateQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                httpTransportSettings,
                CancellationToken.None,
                pageSize);
        }

        /// <summary>
        /// Retrieve the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will return the DeviceRegistrationState for the provided id. It will retrieve
        ///     the correspondent DeviceRegistrationState from the Device Provisioning Service, and return it in the
        ///     <see cref="DeviceRegistrationState"/> object.
        ///
        /// If the id does not exist, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="id">the <c>string</c> that identifies the DeviceRegistrationState. It cannot be <c>null</c> or empty.</param>
        /// <returns>The <see cref="DeviceRegistrationState"/> with the content of the DeviceRegistrationState in the Provisioning Device Service.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to retrieve the DeviceRegistrationState information for the provided registrationId.</exception>
        public Task<DeviceRegistrationState> GetDeviceRegistrationStateAsync(string id)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_023: [The getDeviceRegistrationState shall retrieve the DeviceRegistrationState information for the provided id by calling the get in the RegistrationStatusManager.] */
            return RegistrationStatusManager.GetAsync(_contractApiHttp, id, CancellationToken.None);
        }

        /// <summary>
        /// Retrieve the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will return the DeviceRegistrationState for the provided id. It will retrieve
        ///     the correspondent DeviceRegistrationState from the Device Provisioning Service, and return it in the
        ///     <see cref="DeviceRegistrationState"/> object.
        ///
        /// If the id does not exist, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="id">the <c>string</c> that identifies the DeviceRegistrationState. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="DeviceRegistrationState"/> with the content of the DeviceRegistrationState in the Provisioning Device Service.</returns>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to retrieve the DeviceRegistrationState information for the provided registrationId.</exception>
        public Task<DeviceRegistrationState> GetDeviceRegistrationStateAsync(string id, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_023: [The getDeviceRegistrationState shall retrieve the DeviceRegistrationState information for the provided id by calling the get in the RegistrationStatusManager.] */
            return RegistrationStatusManager.GetAsync(_contractApiHttp, id, cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        ///     provided <see cref="DeviceRegistrationState"/> information. The Device Provisioning Service will care about the
        ///     id and the eTag on the DeviceRegistrationState. If you want to delete the DeviceRegistrationState regardless the
        ///     eTag, you can use the <see cref="DeleteDeviceRegistrationStateAsync(string)"/> passing only the id.
        ///
        /// If the id does not exists or the eTag does not matches, this method will throw
        ///     <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="deviceRegistrationState">the <see cref="DeviceRegistrationState"/> that identifies the DeviceRegistrationState. It cannot be <c>null</c>.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the registration status information for the provided DeviceRegistrationState.</exception>
        public Task DeleteDeviceRegistrationStateAsync(DeviceRegistrationState deviceRegistrationState)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_024: [The deleteDeviceRegistrationState shall delete the DeviceRegistrationState for the provided DeviceRegistrationState by calling the delete in the registrationStatusManager.] */
            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, deviceRegistrationState, CancellationToken.None);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        ///     provided <see cref="DeviceRegistrationState"/> information. The Device Provisioning Service will care about the
        ///     id and the eTag on the DeviceRegistrationState. If you want to delete the DeviceRegistrationState regardless the
        ///     eTag, you can use the <see cref="DeleteDeviceRegistrationStateAsync(string)"/> passing only the id.
        ///
        /// If the id does not exists or the eTag does not matches, this method will throw
        ///     <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="deviceRegistrationState">the <see cref="DeviceRegistrationState"/> that identifies the DeviceRegistrationState. It cannot be <c>null</c>.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the registration status information for the provided DeviceRegistrationState.</exception>

        public Task DeleteDeviceRegistrationStateAsync(DeviceRegistrationState deviceRegistrationState, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_024: [The deleteDeviceRegistrationState shall delete the DeviceRegistrationState for the provided DeviceRegistrationState by calling the delete in the registrationStatusManager.] */
            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, deviceRegistrationState, cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        ///     provided id. It will delete the registration status regardless the eTag. It means that this API
        ///     correspond to the <see cref="DeleteDeviceRegistrationStateAsync(string, string)"/> with the <c>eTag="*"</c>.
        ///
        /// If the id does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="id">the <c>string</c> that identifies the DeviceRegistrationState. It cannot be <c>null</c> or empty.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the DeviceRegistrationState information for the provided registrationId.</exception>
        public Task DeleteDeviceRegistrationStateAsync(string id)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_025: [The deleteDeviceRegistrationState shall delete the DeviceRegistrationState for the provided id by calling the delete in the registrationStatusManager.] */
            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, id, CancellationToken.None);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the DeviceRegistrationState from the Device Provisioning Service using the
        ///     provided id. It will delete the registration status regardless the eTag. It means that this API
        ///     correspond to the <see cref="DeleteDeviceRegistrationStateAsync(string, string)"/> with the <c>eTag="*"</c>.
        ///
        /// If the id does not exists, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="id">the <c>string</c> that identifies the DeviceRegistrationState. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the DeviceRegistrationState information for the provided registrationId.</exception>
        public Task DeleteDeviceRegistrationStateAsync(string id, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_025: [The deleteDeviceRegistrationState shall delete the DeviceRegistrationState for the provided id by calling the delete in the registrationStatusManager.] */
            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, id, cancellationToken);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the registration status from the Device Provisioning Service using the
        ///     provided id and eTag. If you want to delete the registration status regardless the eTag, you can
        ///     use <see cref="DeleteDeviceRegistrationStateAsync(string)"/> or you can pass the eTag as <c>null</c>, empty, or
        ///     <c>"*"</c>.
        ///
        /// If the id does not exists or the eTag does not matches, this method will throw
        ///     <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="id">the <c>string</c> that identifies the DeviceRegistrationState. It cannot be <c>null</c> or empty.</param>
        /// <param name="eTag">the <c>string</c> with the DeviceRegistrationState eTag. It can be <c>null</c> or empty.
        ///             The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the DeviceRegistrationState information for the provided registrationId and eTag.</exception>
        public Task DeleteDeviceRegistrationStateAsync(string id, string eTag)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_026: [The deleteDeviceRegistrationState shall delete the DeviceRegistrationState for the provided id and eTag by calling the delete in the registrationStatusManager.] */
            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, id, CancellationToken.None, eTag);
        }

        /// <summary>
        /// Delete the registration status information.
        /// </summary>
        /// <remarks>
        /// This method will remove the registration status from the Device Provisioning Service using the
        ///     provided id and eTag. If you want to delete the registration status regardless the eTag, you can
        ///     use <see cref="DeleteDeviceRegistrationStateAsync(string)"/> or you can pass the eTag as <c>null</c>, empty, or
        ///     <c>"*"</c>.
        ///
        /// If the id does not exists or the eTag does not matches, this method will throw
        ///     <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="id">the <c>string</c> that identifies the DeviceRegistrationState. It cannot be <c>null</c> or empty.</param>
        /// <param name="eTag">the <c>string</c> with the DeviceRegistrationState eTag. It can be <c>null</c> or empty.
        ///             The Device Provisioning Service will ignore it in all of these cases.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <exception cref="ProvisioningServiceClientException">if the Provisioning Device Service was not able to delete the DeviceRegistrationState information for the provided registrationId and eTag.</exception>
        public Task DeleteDeviceRegistrationStateAsync(string id, string eTag, CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_026: [The deleteDeviceRegistrationState shall delete the DeviceRegistrationState for the provided id and eTag by calling the delete in the registrationStatusManager.] */
            return RegistrationStatusManager.DeleteAsync(_contractApiHttp, id, cancellationToken, eTag);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        ///     Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        public Query CreateEnrollmentGroupRegistrationStateQuery(QuerySpecification querySpecification, string enrollmentGroupId)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_027: [The createEnrollmentGroupRegistrationStateQuery shall create a new DeviceRegistrationState query by calling the createQuery in the registrationStatusManager.] */
            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                CancellationToken.None,
                enrollmentGroupId);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        ///     Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        public Query CreateEnrollmentGroupRegistrationStateQuery(QuerySpecification querySpecification, string enrollmentGroupId, HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_027: [The createEnrollmentGroupRegistrationStateQuery shall create a new DeviceRegistrationState query by calling the createQuery in the registrationStatusManager.] */
            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                httpTransportSettings,
                CancellationToken.None,
                enrollmentGroupId);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        ///     Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        public Query CreateEnrollmentGroupRegistrationStateQuery(
            QuerySpecification querySpecification,
            string enrollmentGroupId,
            CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_027: [The createEnrollmentGroupRegistrationStateQuery shall create a new DeviceRegistrationState query by calling the createQuery in the registrationStatusManager.] */
            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                cancellationToken,
                enrollmentGroupId);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        ///     Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateEnrollmentGroupRegistrationStateQuery(QuerySpecification querySpecification, string enrollmentGroupId, int pageSize)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_028: [The createEnrollmentGroupRegistrationStateQuery shall create a new DeviceRegistrationState query by calling the createQuery in the registrationStatusManager.] */
            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                CancellationToken.None,
                enrollmentGroupId,
                pageSize);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        ///     Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <param name="httpTransportSettings"> Specifies the HTTP transport settings</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateEnrollmentGroupRegistrationStateQuery(
            QuerySpecification querySpecification,
            string enrollmentGroupId,
            int pageSize,
            HttpTransportSettings httpTransportSettings)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_028: [The createEnrollmentGroupRegistrationStateQuery shall create a new DeviceRegistrationState query by calling the createQuery in the registrationStatusManager.] */
            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                httpTransportSettings,
                CancellationToken.None,
                enrollmentGroupId,
                pageSize);
        }

        /// <summary>
        /// Factory to create a registration status query.
        /// </summary>
        /// <remarks>
        /// This method will create a new registration status query for a specific enrollment group on the Device
        ///     Provisioning Service and return it as a <see cref="Query"/> iterator.
        ///
        /// The Device Provisioning Service expects a SQL query in the <see cref="QuerySpecification"/>, for instance
        ///     <c>"SELECT * FROM enrollments"</c>.
        ///
        /// For each iteration, the Query will return a List of objects correspondent to the query result. The maximum
        ///     number of items per iteration can be specified by the pageSize. It is optional, you can provide <b>0</b> for
        ///     default pageSize or use the API <see cref="CreateIndividualEnrollmentQuery(QuerySpecification)"/>.
        /// </remarks>
        /// <param name="querySpecification">the <see cref="QuerySpecification"/> with the SQL query. It cannot be <c>null</c>.</param>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <param name="pageSize">the <c>int</c> with the maximum number of items per iteration. It can be 0 for default, but not negative.</param>
        /// <returns>The <see cref="Query"/> iterator.</returns>
        /// <exception cref="ArgumentException">if the provided parameters are not correct.</exception>
        public Query CreateEnrollmentGroupRegistrationStateQuery(
            QuerySpecification querySpecification,
            string enrollmentGroupId,
            int pageSize,
            CancellationToken cancellationToken)
        {
            /* SRS_PROVISIONING_SERVICE_CLIENT_21_028: [The createEnrollmentGroupRegistrationStateQuery shall create a new DeviceRegistrationState query by calling the createQuery in the registrationStatusManager.] */
            return RegistrationStatusManager.CreateEnrollmentGroupQuery(
                _hostName,
                GetHeaderProvider(),
                querySpecification,
                new HttpTransportSettings(),
                cancellationToken,
                enrollmentGroupId,
                pageSize);
        }

        /// <summary>
        /// Retrieve the attestation information for an individual enrollment.
        /// </summary>
        /// <remarks>
        /// If the registrationId does not match any individual enrollment, this method will throw a <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="registrationId">The registration Id of the individual enrollment to retrieve the attestation information of. This may not be null or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> of the individual enrollment associated with the provided registrationId.</returns>
        public Task<AttestationMechanism> GetIndividualEnrollmentAttestationAsync(string registrationId, CancellationToken cancellationToken = default)
        {
            return IndividualEnrollmentManager.GetEnrollmentAttestationAsync(_contractApiHttp, registrationId, cancellationToken);
        }

        /// <summary>
        /// Retrieve the enrollmentGroup attestation information.
        /// </summary>
        /// <remarks>
        /// If the provided group id does not match up with any enrollment group, this method will throw <see cref="ProvisioningServiceClientException"/>.
        /// </remarks>
        /// <param name="enrollmentGroupId">the <c>string</c> that identifies the enrollmentGroup. It cannot be <c>null</c> or empty.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The <see cref="AttestationMechanism"/> associated with the provided group Id.</returns>
        public Task<AttestationMechanism> GetEnrollmentGroupAttestationAsync(string enrollmentGroupId, CancellationToken cancellationToken = default)
        {
            return EnrollmentGroupManager.GetEnrollmentAttestationAsync(_contractApiHttp, enrollmentGroupId, cancellationToken);
        }

        internal IAuthorizationHeaderProvider GetHeaderProvider()
        {
            return _provisioningConnectionString
                   ?? (IAuthorizationHeaderProvider) _provisioningTokenCredential
                   ?? _provisioningSasCredential;
        }
    }
}
