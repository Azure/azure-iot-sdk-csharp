// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Exceptions;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Contains methods that services can use to perform create, remove, update and delete operations on devices.
    /// </summary>
    /// <remarks>
    /// For more information, see <see href="https://github.com/Azure/azure-iot-sdk-csharp#iot-hub-service-sdk"/>.
    /// <para>
    /// This client creates lifetime long instances of <see cref="HttpClient"/> that are tied to the URI of the
    /// IoT hub specified, configure any proxy settings, and connection lease timeout.
    /// For that reason, the instances are not static and an application using this client
    /// should create and save it for all use. Repeated creation may cause
    /// <see href="https://docs.microsoft.com/azure/architecture/antipatterns/improper-instantiation/">socket exhaustion</see>.
    /// </para>
    /// </remarks>
    [SuppressMessage(
        "Naming",
        "CA1716:Identifiers should not match keywords",
        Justification = "Cannot change parameter names as it is considered a breaking change.")]
    public class RegistryManager : IDisposable
    {
        private const string RequestUriFormat = "/devices/{0}?{1}";
        private const string JobsUriFormat = "/jobs{0}?{1}";
        private const string DevicesRequestUriFormat = "/devices/?top={0}&{1}";
        private const string DevicesQueryUriFormat = "/devices/query?" + ClientApiVersionHelper.ApiVersionQueryString;
        private const string WildcardEtag = "*";

        private const string ContinuationTokenHeader = "x-ms-continuation";
        private const string PageSizeHeader = "x-ms-max-item-count";

        private const string TwinUriFormat = "/twins/{0}?{1}";

        private const string ModuleTwinUriFormat = "/twins/{0}/modules/{1}?{2}";

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);

        private static readonly Regex s_deviceIdRegex = new Regex(
            @"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            s_regexTimeoutMilliseconds);

        private static readonly TimeSpan s_defaultOperationTimeout = TimeSpan.FromSeconds(100);
        private static readonly TimeSpan s_defaultGetDevicesOperationTimeout = TimeSpan.FromSeconds(120);

        private readonly string _iotHubName;
        private IHttpClientHelper _httpClientHelper;

        /// <summary>
        /// Creates an instance of RegistryManager, provided for unit testing purposes only.
        /// </summary>
        public RegistryManager()
        {
        }

        internal RegistryManager(IotHubConnectionProperties connectionProperties, HttpTransportSettings transportSettings)
        {
            _iotHubName = connectionProperties.IotHubName;
            _httpClientHelper = new HttpClientHelper(
                connectionProperties.HttpsEndpoint,
                connectionProperties,
                ExceptionHandlingHelper.GetDefaultErrorMapping(),
                s_defaultOperationTimeout,
                transportSettings.Proxy,
                transportSettings.ConnectionLeaseTimeoutMilliseconds);
        }

        // internal test helper
        internal RegistryManager(string iotHubName, IHttpClientHelper httpClientHelper)
        {
            _iotHubName = iotHubName;
            _httpClientHelper = httpClientHelper ?? throw new ArgumentNullException(nameof(httpClientHelper));
        }

        /// <summary>
        /// Creates RegistryManager from an IoT hub connection string.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <returns>A RegistryManager instance.</returns>
        public static RegistryManager CreateFromConnectionString(string connectionString)
        {
            return CreateFromConnectionString(connectionString, new HttpTransportSettings());
        }

        /// <summary>
        /// Creates an instance of RegistryManager, authenticating using an IoT hub connection string, and specifying
        /// HTTP transport settings.
        /// </summary>
        /// <param name="connectionString">The IoT hub connection string.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public static RegistryManager CreateFromConnectionString(string connectionString, HttpTransportSettings transportSettings)
        {
            if (transportSettings == null)
            {
                throw new ArgumentNullException(nameof(transportSettings), "The HTTP transport settings cannot be null.");
            }

            var iotHubConnectionString = IotHubConnectionString.Parse(connectionString);
            return new RegistryManager(iotHubConnectionString, transportSettings);
        }

        /// <summary>
        /// Creates RegistryManager, authenticating using an identity in Azure Active Directory (AAD).
        /// </summary>
        /// <remarks>
        /// For more about information on the options of authenticating using a derived instance of <see cref="TokenCredential"/>, see
        /// <see href="https://docs.microsoft.com/dotnet/api/overview/azure/identity-readme"/>.
        /// For more information on configuring IoT hub with Azure Active Directory, see
        /// <see href="https://docs.microsoft.com/azure/iot-hub/iot-hub-dev-guide-azure-ad-rbac"/>
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Azure Active Directory (AAD) credentials to authenticate with IoT hub.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public static RegistryManager Create(
            string hostName,
            TokenCredential credential,
            HttpTransportSettings transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var tokenCredentialProperties = new IotHubTokenCrendentialProperties(hostName, credential);
            return new RegistryManager(tokenCredentialProperties, transportSettings ?? new HttpTransportSettings());
        }

        /// <summary>
        /// Creates RegistryManager using a shared access signature provided and refreshed as necessary by the caller.
        /// </summary>
        /// <remarks>
        /// Users may wish to build their own shared access signature (SAS) tokens rather than give the shared key to the SDK and let it manage signing and renewal.
        /// The <see cref="AzureSasCredential"/> object gives the SDK access to the SAS token, while the caller can update it as necessary using the
        /// <see cref="AzureSasCredential.Update(string)"/> method.
        /// </remarks>
        /// <param name="hostName">IoT hub host name.</param>
        /// <param name="credential">Credential that generates a SAS token to authenticate with IoT hub. See <see cref="AzureSasCredential"/>.</param>
        /// <param name="transportSettings">The HTTP transport settings.</param>
        /// <returns>A RegistryManager instance.</returns>
        public static RegistryManager Create(
            string hostName,
            AzureSasCredential credential,
            HttpTransportSettings transportSettings = default)
        {
            if (string.IsNullOrEmpty(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)},  Parameter cannot be null or empty");
            }

            if (credential == null)
            {
                throw new ArgumentNullException($"{nameof(credential)},  Parameter cannot be null");
            }

            var sasCredentialProperties = new IotHubSasCredentialProperties(hostName, credential);
            return new RegistryManager(sasCredentialProperties, transportSettings ?? new HttpTransportSettings());
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing && _httpClientHelper != null)
            {
                _httpClientHelper.Dispose();
                _httpClientHelper = null;
            }
        }

        /// <summary>
        /// Explicitly open the RegistryManager instance.
        /// </summary>
        public virtual Task OpenAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Closes the RegistryManager instance and disposes its resources.
        /// </summary>
        public virtual Task CloseAsync()
        {
            return TaskHelpers.CompletedTask;
        }

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        public virtual IQuery CreateQuery(string sqlQueryString)
        {
            return CreateQuery(sqlQueryString, null);
        }

        /// <summary>
        /// Retrieves a handle through which a result for a given query can be fetched.
        /// </summary>
        /// <param name="sqlQueryString">The SQL query.</param>
        /// <param name="pageSize">The maximum number of items per page.</param>
        /// <returns>A handle used to fetch results for a SQL query.</returns>
        public virtual IQuery CreateQuery(string sqlQueryString, int? pageSize)
        {
            Logging.Enter(this, $"Creating query", nameof(CreateQuery));
            try
            {
                return new Query((token) => ExecuteQueryAsync(
                    sqlQueryString,
                    pageSize,
                    token,
                    CancellationToken.None));
            }
            catch (Exception ex)
            {
                Logging.Error(this, $"{nameof(CreateQuery)} threw an exception: {ex}", nameof(CreateQuery));
                throw;
            }
            finally
            {
                Logging.Exit(this, $"Creating query", nameof(CreateQuery));
            }
        }

        /// <summary>
        /// Gets <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId)
        {
            return GetTwinAsync(deviceId, CancellationToken.None);
        }

        /// <summary>
        /// Gets <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device twin on device: {deviceId}", nameof(GetTwinAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.NotFound, async responseMessage => new DeviceNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) }
                };

                return _httpClientHelper.GetAsync<Twin>(GetTwinUri(deviceId), errorMappingOverrides, null, false, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetTwinAsync)} threw an exception: {ex}", nameof(GetTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting device twin on device: {deviceId}", nameof(GetTwinAsync));
            }
        }

        /// <summary>
        /// Gets Module's <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId, string moduleId)
        {
            return GetTwinAsync(deviceId, moduleId, CancellationToken.None);
        }

        /// <summary>
        /// Gets Module's <see cref="Twin"/> from IotHub
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Twin instance.</returns>
        public virtual Task<Twin> GetTwinAsync(string deviceId, string moduleId, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Getting device twin on device: {deviceId} and module: {moduleId}", nameof(GetTwinAsync));

            try
            {
                if (string.IsNullOrWhiteSpace(deviceId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "deviceId"));
                }

                if (string.IsNullOrWhiteSpace(moduleId))
                {
                    throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrWhitespace, "moduleId"));
                }

                EnsureInstanceNotClosed();
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>()
                {
                    { HttpStatusCode.NotFound, async responseMessage => new ModuleNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false), (Exception)null) }
                };

                return _httpClientHelper.GetAsync<Twin>(GetModuleTwinRequestUri(deviceId, moduleId), errorMappingOverrides, null, false, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(GetTwinAsync)} threw an exception: {ex}", nameof(GetTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Getting device twin on device: {deviceId} and module: {moduleId}", nameof(GetTwinAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, twinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, Twin twinPatch, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, twinPatch, etag, false, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string jsonTwinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, jsonTwinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string jsonTwinPatch, string etag, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin on device: {deviceId}", nameof(UpdateTwinAsync));

            try
            {
                if (string.IsNullOrWhiteSpace(jsonTwinPatch))
                {
                    throw new ArgumentNullException(nameof(jsonTwinPatch));
                }

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(jsonTwinPatch);
                return UpdateTwinAsync(deviceId, twin, etag, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(UpdateTwinAsync)} threw an exception: {ex}", nameof(UpdateTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating device twin on device: {deviceId}", nameof(UpdateTwinAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, Twin twinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, moduleId, twinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="twinPatch">Twin with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, Twin twinPatch, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, moduleId, twinPatch, etag, false, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag)
        {
            return UpdateTwinAsync(deviceId, moduleId, jsonTwinPatch, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="jsonTwinPatch">Twin json with updated fields.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> UpdateTwinAsync(string deviceId, string moduleId, string jsonTwinPatch, string etag, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Updating device twin on device: {deviceId} and module: {moduleId}", nameof(UpdateTwinAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(jsonTwinPatch))
                {
                    throw new ArgumentNullException(nameof(jsonTwinPatch));
                }

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(jsonTwinPatch);
                return UpdateTwinAsync(deviceId, moduleId, twin, etag, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(UpdateTwinAsync)} threw an exception: {ex}", nameof(UpdateTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Updating device twin on device: {deviceId} and module: {moduleId}", nameof(UpdateTwinAsync));
            }
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwinsAsync(IEnumerable<Twin> twins)
        {
            return UpdateTwinsAsync(twins, false, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwinsAsync(IEnumerable<Twin> twins, CancellationToken cancellationToken)
        {
            return UpdateTwinsAsync(twins, false, cancellationToken);
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwinsAsync(IEnumerable<Twin> twins, bool forceUpdate)
        {
            return UpdateTwinsAsync(twins, forceUpdate, CancellationToken.None);
        }

        /// <summary>
        /// Update the mutable fields for a list of <see cref="Twin"/>s previously created within the system
        /// </summary>
        /// <param name="twins">List of <see cref="Twin"/>s with updated fields.</param>
        /// <param name="forceUpdate">Forces the <see cref="Twin"/> object to be updated even if it has changed since it was retrieved last time.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Result of the bulk update operation.</returns>
        public virtual Task<BulkRegistryOperationResult> UpdateTwinsAsync(IEnumerable<Twin> twins, bool forceUpdate, CancellationToken cancellationToken)
        {
            return BulkDeviceOperationsAsync<BulkRegistryOperationResult>(
                GenerateExportImportDeviceListForTwinBulkOperations(twins, forceUpdate ? ImportMode.UpdateTwin : ImportMode.UpdateTwinIfMatchETag),
                ClientApiVersionHelper.ApiVersionQueryString,
                cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, Twin newTwin, string etag)
        {
            return ReplaceTwinAsync(deviceId, newTwin, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, Twin newTwin, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, newTwin, etag, true, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string newTwinJson, string etag)
        {
            return ReplaceTwinAsync(deviceId, newTwinJson, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string newTwinJson, string etag, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceTwinAsync));
            try
            {
                if (string.IsNullOrWhiteSpace(newTwinJson))
                {
                    throw new ArgumentNullException(nameof(newTwinJson));
                }

                // TODO: Do we need to deserialize Twin, only to serialize it again?
                Twin twin = JsonConvert.DeserializeObject<Twin>(newTwinJson);
                return ReplaceTwinAsync(deviceId, twin, etag, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(ReplaceTwinAsync)} threw an exception: {ex}", nameof(ReplaceTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId}", nameof(ReplaceTwinAsync));
            }
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, Twin newTwin, string etag)
        {
            return ReplaceTwinAsync(deviceId, moduleId, newTwin, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwin">New Twin object to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, Twin newTwin, string etag, CancellationToken cancellationToken)
        {
            return UpdateTwinInternalAsync(deviceId, moduleId, newTwin, etag, true, cancellationToken);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, string newTwinJson, string etag)
        {
            return ReplaceTwinAsync(deviceId, moduleId, newTwinJson, etag, CancellationToken.None);
        }

        /// <summary>
        /// Updates the mutable fields of Module's <see cref="Twin"/>
        /// </summary>
        /// <param name="deviceId">The device Id.</param>
        /// <param name="moduleId">The module Id.</param>
        /// <param name="newTwinJson">New Twin json to replace with.</param>
        /// <param name="etag">Twin's ETag.</param>
        /// <param name="cancellationToken">Task cancellation token.</param>
        /// <returns>Updated Twin instance.</returns>
        public virtual Task<Twin> ReplaceTwinAsync(string deviceId, string moduleId, string newTwinJson, string etag, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(newTwinJson))
            {
                throw new ArgumentNullException(nameof(newTwinJson));
            }

            // TODO: Do we need to deserialize Twin, only to serialize it again?
            Twin twin = JsonConvert.DeserializeObject<Twin>(newTwinJson);
            return ReplaceTwinAsync(deviceId, moduleId, twin, etag, cancellationToken);
        }

        private Task<Twin> UpdateTwinInternalAsync(string deviceId, Twin twin, string etag, bool isReplace, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            if (twin != null)
            {
                twin.DeviceId = deviceId;
            }

            ValidateTwinId(twin);

            if (string.IsNullOrEmpty(etag))
            {
                throw new ArgumentNullException(nameof(etag));
            }

            var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
            {
                {
                    HttpStatusCode.PreconditionFailed,
                    async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                },
                {
                    HttpStatusCode.NotFound,
                    async responseMessage => new DeviceNotFoundException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false), (Exception)null)
                }
            };

            return isReplace
                ? _httpClientHelper.PutAsync<Twin, Twin>(
                    GetTwinUri(deviceId),
                    twin,
                    etag,
                    etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                    errorMappingOverrides,
                    cancellationToken)
                : _httpClientHelper.PatchAsync<Twin, Twin>(
                    GetTwinUri(deviceId),
                    twin,
                    etag,
                    etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                    errorMappingOverrides,
                    cancellationToken);
        }

        private Task<Twin> UpdateTwinInternalAsync(string deviceId, string moduleId, Twin twin, string etag, bool isReplace, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Replacing device twin on device: {deviceId} - module: {moduleId} - is replace: {isReplace}", nameof(UpdateTwinAsync));
            try
            {
                EnsureInstanceNotClosed();

                if (twin != null)
                {
                    twin.DeviceId = deviceId;
                    twin.ModuleId = moduleId;
                }

                ValidateTwinId(twin);

                if (string.IsNullOrEmpty(etag))
                {
                    throw new ArgumentNullException(nameof(etag));
                }

                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    {
                        HttpStatusCode.PreconditionFailed,
                        async responseMessage => new PreconditionFailedException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false))
                    },
                    {
                        HttpStatusCode.NotFound,
                        async responseMessage => new ModuleNotFoundException(
                            await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false),
                            (Exception)null)
                    }
                };

                return isReplace
                    ? _httpClientHelper.PutAsync<Twin, Twin>(
                        GetModuleTwinRequestUri(deviceId, moduleId),
                        twin,
                        etag,
                        etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                        errorMappingOverrides,
                        cancellationToken)
                    : _httpClientHelper.PatchAsync<Twin, Twin>(
                        GetModuleTwinRequestUri(deviceId, moduleId),
                        twin,
                        etag,
                        etag == WildcardEtag ? PutOperationType.ForceUpdateEntity : PutOperationType.UpdateEntity,
                        errorMappingOverrides,
                        cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(UpdateTwinAsync)} threw an exception: {ex}", nameof(UpdateTwinAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Replacing device twin on device: {deviceId} - module: {moduleId} - is replace: {isReplace}", nameof(UpdateTwinAsync));
            }
        }

        private async Task<QueryResult> ExecuteQueryAsync(string sqlQueryString, int? pageSize, string continuationToken, CancellationToken cancellationToken)
        {
            EnsureInstanceNotClosed();

            if (string.IsNullOrWhiteSpace(sqlQueryString))
            {
                throw new ArgumentException(IotHubApiResources.GetString(ApiResources.ParameterCannotBeNullOrEmpty, nameof(sqlQueryString)));
            }

            var customHeaders = new Dictionary<string, string>();
            if (!string.IsNullOrWhiteSpace(continuationToken))
            {
                customHeaders.Add(ContinuationTokenHeader, continuationToken);
            }

            if (pageSize != null)
            {
                customHeaders.Add(PageSizeHeader, pageSize.ToString());
            }

            HttpResponseMessage response = await _httpClientHelper
                .PostAsync(
                    QueryDevicesRequestUri(),
                    new QuerySpecification { Sql = sqlQueryString },
                    null,
                    customHeaders,
                    new MediaTypeHeaderValue("application/json") { CharSet = "utf-8" },
                    null,
                    cancellationToken)
                .ConfigureAwait(false);

            return await QueryResult.FromHttpResponseAsync(response).ConfigureAwait(false);
        }

        private static Uri GetModuleTwinRequestUri(string deviceId, string moduleId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            moduleId = WebUtility.UrlEncode(moduleId);
            return new Uri(ModuleTwinUriFormat.FormatInvariant(deviceId, moduleId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetJobUri(string jobId, string apiVersion = ClientApiVersionHelper.ApiVersionQueryString)
        {
            return new Uri(JobsUriFormat.FormatInvariant(jobId, apiVersion), UriKind.Relative);
        }

        private static Uri GetDevicesRequestUri(int maxCount)
        {
            return new Uri(DevicesRequestUriFormat.FormatInvariant(maxCount, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static Uri GetBulkRequestUri(string apiVersionQueryString)
        {
            return new Uri(RequestUriFormat.FormatInvariant(string.Empty, apiVersionQueryString), UriKind.Relative);
        }

        private static Uri QueryDevicesRequestUri()
        {
            return new Uri(DevicesQueryUriFormat, UriKind.Relative);
        }

        private static Uri GetTwinUri(string deviceId)
        {
            deviceId = WebUtility.UrlEncode(deviceId);
            return new Uri(TwinUriFormat.FormatInvariant(deviceId, ClientApiVersionHelper.ApiVersionQueryString), UriKind.Relative);
        }

        private static void ValidateDeviceId(Device device)
        {
            if (device == null)
            {
                throw new ArgumentNullException(nameof(device));
            }

            if (string.IsNullOrWhiteSpace(device.Id))
            {
                throw new ArgumentException("device.Id");
            }

            if (!s_deviceIdRegex.IsMatch(device.Id))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(device.Id));
            }
        }

        private static void ValidateTwinId(Twin twin)
        {
            if (twin == null)
            {
                throw new ArgumentNullException(nameof(twin));
            }

            if (string.IsNullOrWhiteSpace(twin.DeviceId))
            {
                throw new ArgumentException("twin.DeviceId");
            }

            if (!s_deviceIdRegex.IsMatch(twin.DeviceId))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(twin.DeviceId));
            }
        }

        private static void ValidateModuleId(Module module)
        {
            if (module == null)
            {
                throw new ArgumentNullException(nameof(module));
            }

            if (string.IsNullOrWhiteSpace(module.DeviceId))
            {
                throw new ArgumentException("module.Id");
            }

            if (string.IsNullOrWhiteSpace(module.Id))
            {
                throw new ArgumentException("module.ModuleId");
            }

            if (!s_deviceIdRegex.IsMatch(module.DeviceId))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(module.DeviceId));
            }

            if (!s_deviceIdRegex.IsMatch(module.Id))
            {
                throw new ArgumentException(ApiResources.DeviceIdInvalid.FormatInvariant(module.Id));
            }
        }

        private void EnsureInstanceNotClosed()
        {
            if (_httpClientHelper == null)
            {
                throw new ObjectDisposedException("RegistryManager", ApiResources.RegistryManagerInstanceAlreadyClosed);
            }
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForBulkOperations(IEnumerable<Device> devices, ImportMode importMode)
        {
            if (devices == null)
            {
                throw new ArgumentNullException(nameof(devices));
            }

            if (!devices.Any())
            {
                throw new ArgumentException($"Parameter {nameof(devices)} cannot be empty.");
            }

            var exportImportDeviceList = new List<ExportImportDevice>(devices.Count());
            foreach (Device device in devices)
            {
                ValidateDeviceId(device);

                switch (importMode)
                {
                    case ImportMode.Create:
                        if (!string.IsNullOrWhiteSpace(device.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagSetWhileRegisteringDevice);
                        }
                        break;

                    case ImportMode.Update:
                        // No preconditions
                        break;

                    case ImportMode.UpdateIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingDevice);
                        }
                        break;

                    case ImportMode.Delete:
                        // No preconditions
                        break;

                    case ImportMode.DeleteIfMatchETag:
                        if (string.IsNullOrWhiteSpace(device.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagNotSetWhileDeletingDevice);
                        }
                        break;

                    default:
                        throw new ArgumentException(IotHubApiResources.GetString(ApiResources.InvalidImportMode, importMode));
                }

                var exportImportDevice = new ExportImportDevice(device, importMode);
                exportImportDeviceList.Add(exportImportDevice);
            }

            return exportImportDeviceList;
        }

        private static IEnumerable<ExportImportDevice> GenerateExportImportDeviceListForTwinBulkOperations(IEnumerable<Twin> twins, ImportMode importMode)
        {
            if (twins == null)
            {
                throw new ArgumentNullException(nameof(twins));
            }

            if (!twins.Any())
            {
                throw new ArgumentException($"Parameter {nameof(twins)} cannot be empty");
            }

            var exportImportDeviceList = new List<ExportImportDevice>(twins.Count());
            foreach (Twin twin in twins)
            {
                ValidateTwinId(twin);

                switch (importMode)
                {
                    case ImportMode.UpdateTwin:
                        // No preconditions
                        break;

                    case ImportMode.UpdateTwinIfMatchETag:
                        if (string.IsNullOrWhiteSpace(twin.ETag))
                        {
                            throw new ArgumentException(ApiResources.ETagNotSetWhileUpdatingTwin);
                        }
                        break;

                    default:
                        throw new ArgumentException(IotHubApiResources.GetString(ApiResources.InvalidImportMode, importMode));
                }

                var exportImportDevice = new ExportImportDevice
                {
                    Id = twin.DeviceId,
                    ModuleId = twin.ModuleId,
                    ImportMode = importMode,
                    TwinETag = importMode == ImportMode.UpdateTwinIfMatchETag ? twin.ETag : null,
                    Tags = twin.Tags,
                    Properties = new ExportImportDevice.PropertyContainer(),
                };
                exportImportDevice.Properties.DesiredProperties = twin.Properties?.Desired;

                exportImportDeviceList.Add(exportImportDevice);
            }

            return exportImportDeviceList;
        }

        private Task<T> BulkDeviceOperationsAsync<T>(IEnumerable<ExportImportDevice> devices, string version, CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"Performing bulk device operation on : {devices?.Count()} devices. version: {version}", nameof(BulkDeviceOperationsAsync));
            try
            {
                var errorMappingOverrides = new Dictionary<HttpStatusCode, Func<HttpResponseMessage, Task<Exception>>>
                {
                    { HttpStatusCode.PreconditionFailed, async responseMessage => new PreconditionFailedException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    { HttpStatusCode.RequestEntityTooLarge, async responseMessage => new TooManyDevicesException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) },
                    { HttpStatusCode.BadRequest, async responseMessage => new ArgumentException(await ExceptionHandlingHelper.GetExceptionMessageAsync(responseMessage).ConfigureAwait(false)) }
                };

                return _httpClientHelper.PostAsync<IEnumerable<ExportImportDevice>, T>(GetBulkRequestUri(version), devices, errorMappingOverrides, null, cancellationToken);
            }
            catch (Exception ex)
            {
                if (Logging.IsEnabled)
                    Logging.Error(this, $"{nameof(BulkDeviceOperationsAsync)} threw an exception: {ex}", nameof(BulkDeviceOperationsAsync));
                throw;
            }
            finally
            {
                if (Logging.IsEnabled)
                    Logging.Exit(this, $"Performing bulk device operation on : {devices?.Count()} devices. version: {version}", nameof(BulkDeviceOperationsAsync));
            }
        }
    }
}
