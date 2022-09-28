// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Rest;
using Microsoft.Rest.Serialization;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    /// <summary>
    /// RuntimeRegistration operations.
    /// </summary>
    internal partial class RuntimeRegistration : IServiceOperations<DeviceProvisioningServiceRuntimeClient>, IRuntimeRegistration
    {
        /// <summary>
        /// Creates an instance of the RuntimeRegistration class.
        /// </summary>
        /// <param name='client'>
        /// Reference to the service client.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        public RuntimeRegistration(DeviceProvisioningServiceRuntimeClient client)
        {
            Client = client ?? throw new ArgumentNullException(nameof(client));
        }

        /// <summary>
        /// Gets a reference to the DeviceProvisioningServiceRuntimeClient
        /// </summary>
        public DeviceProvisioningServiceRuntimeClient Client { get; private set; }

        /// <summary>
        /// Gets the registration operation status.
        /// </summary>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='operationId'>
        /// Operation ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='customHeaders'>
        /// Headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <return>
        /// A response object containing the response body and response headers.
        /// </return>
        public async Task<HttpOperationResponse<RegistrationOperationStatus>> OperationStatusLookupWithHttpMessagesAsync(
            string registrationId,
            string operationId,
            string idScope,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            if (registrationId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "registrationId");
            }
            if (operationId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "operationId");
            }
            if (idScope == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "idScope");
            }
            // Tracing
            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString(CultureInfo.InvariantCulture);
                var tracingParameters = new Dictionary<string, object>
                {
                    { "registrationId", registrationId },
                    { "operationId", operationId },
                    { "idScope", idScope },
                    { "cancellationToken", cancellationToken }
                };
                ServiceClientTracing.Enter(invocationId, this, "OperationStatusLookup", tracingParameters);
            }
            // Construct URL
            string baseUrl = Client.BaseUri.AbsoluteUri;
            string url = new Uri(
                new Uri(baseUrl + (baseUrl.EndsWith("/", StringComparison.Ordinal) ? "" : "/")),
                "{idScope}/registrations/{registrationId}/operations/{operationId}").ToString();

            url = url.Replace("{registrationId}", Uri.EscapeDataString(registrationId));
            url = url.Replace("{operationId}", Uri.EscapeDataString(operationId));
            url = url.Replace("{idScope}", Uri.EscapeDataString(idScope));

            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod("GET"),
                RequestUri = new Uri(url)
            };

            // Set Headers
            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, List<string>> header in customHeaders)
                {
                    if (httpRequest.Headers.Contains(header.Key))
                    {
                        httpRequest.Headers.Remove(header.Key);
                    }
                    httpRequest.Headers.TryAddWithoutValidation(header.Key, header.Value);
                }
            }

            // Serialize Request
            string requestContent = null;
            // Set Credentials
            if (Client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage httpResponse = await Client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, httpResponse);
            }
            HttpStatusCode statusCode = httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string responseContent;
            if ((int)statusCode != 200 && (int)statusCode != 202)
            {
                var ex = new HttpOperationException(string.Format(CultureInfo.InvariantCulture, "Operation returned an invalid status code '{0}'", statusCode));
                responseContent = httpResponse.Content == null
                    ? string.Empty
                    : await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                ex.Request = new HttpRequestMessageWrapper(httpRequest, requestContent);
                ex.Response = new HttpResponseMessageWrapper(httpResponse, responseContent);
                if (shouldTrace)
                {
                    ServiceClientTracing.Error(invocationId, ex);
                }
                httpRequest.Dispose();
                if (httpResponse != null)
                {
                    httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var result = new HttpOperationResponse<RegistrationOperationStatus>
            {
                Request = httpRequest,
                Response = httpResponse
            };
            // Deserialize Response
            if ((int)statusCode == 200)
            {
                responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    result.Body = SafeJsonConvert.DeserializeObject<RegistrationOperationStatus>(responseContent, Client.DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    httpRequest.Dispose();
                    if (httpResponse != null)
                    {
                        httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", responseContent, ex);
                }
            }
            // Deserialize Response
            if ((int)statusCode == 202)
            {
                responseContent = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    result.Body = SafeJsonConvert.DeserializeObject<RegistrationOperationStatus>(responseContent, Client.DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    httpRequest.Dispose();
                    if (httpResponse != null)
                    {
                        httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", responseContent, ex);
                }
            }
            if (shouldTrace)
            {
                ServiceClientTracing.Exit(invocationId, result);
            }
            return result;
        }

        /// <summary>
        /// Gets the device registration status.
        /// </summary>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration
        /// </param>
        /// <param name='customHeaders'>
        /// Headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <return>
        /// A response object containing the response body and response headers.
        /// </return>
        public async Task<HttpOperationResponse<DeviceRegistrationResult>> DeviceRegistrationStatusLookupWithHttpMessagesAsync(
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default,
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default)
        {
            if (registrationId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "registrationId");
            }
            if (idScope == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "idScope");
            }
            // Tracing
            bool shouldTrace = ServiceClientTracing.IsEnabled;
            string invocationId = null;
            if (shouldTrace)
            {
                invocationId = ServiceClientTracing.NextInvocationId.ToString(CultureInfo.InvariantCulture);
                var tracingParameters = new Dictionary<string, object>
                {
                    { "registrationId", registrationId },
                    { "deviceRegistration", deviceRegistration },
                    { "idScope", idScope },
                    { "cancellationToken", cancellationToken }
                };
                ServiceClientTracing.Enter(invocationId, this, "DeviceRegistrationStatusLookup", tracingParameters);
            }
            // Construct URL
            string baseUrl = Client.BaseUri.AbsoluteUri;
            string url = new Uri(
                    new Uri(baseUrl + (baseUrl.EndsWith("/", StringComparison.Ordinal) ? "" : "/")),
                    "{idScope}/registrations/{registrationId}")
                .ToString();

            url = url.Replace("{registrationId}", Uri.EscapeDataString(registrationId));
            url = url.Replace("{idScope}", Uri.EscapeDataString(idScope));

            // Create HTTP transport objects
            var httpRequest = new HttpRequestMessage
            {
                Method = new HttpMethod("POST"),
                RequestUri = new Uri(url)
            };
            // Set Headers

            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, List<string>> _header in customHeaders)
                {
                    if (httpRequest.Headers.Contains(_header.Key))
                    {
                        httpRequest.Headers.Remove(_header.Key);
                    }
                    httpRequest.Headers.TryAddWithoutValidation(_header.Key, _header.Value);
                }
            }

            // Serialize Request
            string requestContent = null;
            if (deviceRegistration != null)
            {
                requestContent = SafeJsonConvert.SerializeObject(deviceRegistration, Client.SerializationSettings);
                httpRequest.Content = new StringContent(requestContent, Encoding.UTF8);
                httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
            }
            // Set Credentials
            if (Client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Client.Credentials.ProcessHttpRequestAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            if (shouldTrace)
            {
                ServiceClientTracing.SendRequest(invocationId, httpRequest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            HttpResponseMessage _httpResponse = await Client.HttpClient.SendAsync(httpRequest, cancellationToken).ConfigureAwait(false);
            if (shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(invocationId, _httpResponse);
            }
            HttpStatusCode _statusCode = _httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string _responseContent = null;
            if ((int)_statusCode != 200)
            {
                var ex = new HttpOperationException(string.Format(CultureInfo.InvariantCulture, "Operation returned an invalid status code '{0}'", _statusCode));
                if (_httpResponse.Content != null)
                {
                    _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    _responseContent = string.Empty;
                }
                ex.Request = new HttpRequestMessageWrapper(httpRequest, requestContent);
                ex.Response = new HttpResponseMessageWrapper(_httpResponse, _responseContent);
                if (shouldTrace)
                {
                    ServiceClientTracing.Error(invocationId, ex);
                }
                httpRequest.Dispose();
                if (_httpResponse != null)
                {
                    _httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var _result = new HttpOperationResponse<DeviceRegistrationResult>
            {
                Request = httpRequest,
                Response = _httpResponse
            };
            // Deserialize Response
            if ((int)_statusCode == 200)
            {
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    _result.Body = SafeJsonConvert.DeserializeObject<DeviceRegistrationResult>(_responseContent, Client.DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }
            if (shouldTrace)
            {
                ServiceClientTracing.Exit(invocationId, _result);
            }
            return _result;
        }

        /// <summary>
        /// Registers the devices.
        /// </summary>
        /// <param name='registrationId'>
        /// Registration ID.
        /// </param>
        /// <param name='idScope'>
        /// </param>
        /// <param name='deviceRegistration'>
        /// Device registration request.
        /// </param>
        /// <param name='forceRegistration'>
        /// Force the device to re-register. Setting this option may assign the device
        /// to a different IotHub.
        /// </param>
        /// <param name='customHeaders'>
        /// Headers that will be added to request.
        /// </param>
        /// <param name='cancellationToken'>
        /// The cancellation token.
        /// </param>
        /// <exception cref="HttpOperationException">
        /// Thrown when the operation returned an invalid status code
        /// </exception>
        /// <exception cref="SerializationException">
        /// Thrown when unable to deserialize the response
        /// </exception>
        /// <exception cref="ValidationException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// Thrown when a required parameter is null
        /// </exception>
        /// <return>
        /// A response object containing the response body and response headers.
        /// </return>
        public async Task<HttpOperationResponse<RegistrationOperationStatus>> RegisterDeviceWithHttpMessagesAsync(
            string registrationId,
            string idScope,
            DeviceRegistration deviceRegistration = default(DeviceRegistration),
            bool? forceRegistration = default(bool?),
            Dictionary<string, List<string>> customHeaders = null,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            if (registrationId == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "registrationId");
            }
            if (idScope == null)
            {
                throw new ValidationException(ValidationRules.CannotBeNull, "idScope");
            }
            // Tracing
            bool _shouldTrace = ServiceClientTracing.IsEnabled;
            string _invocationId = null;
            if (_shouldTrace)
            {
                _invocationId = ServiceClientTracing.NextInvocationId.ToString(CultureInfo.InvariantCulture);
                var tracingParameters = new Dictionary<string, object>
                {
                    { "registrationId", registrationId },
                    { "deviceRegistration", deviceRegistration },
                    { "forceRegistration", forceRegistration },
                    { "idScope", idScope },
                    { "cancellationToken", cancellationToken }
                };
                ServiceClientTracing.Enter(_invocationId, this, "RegisterDevice", tracingParameters);
            }
            // Construct URL
            string baseUrl = Client.BaseUri.AbsoluteUri;
            string url = new Uri(
                    new Uri(baseUrl + (baseUrl.EndsWith("/", StringComparison.Ordinal) ? "" : "/")),
                    "{idScope}/registrations/{registrationId}/register")
                .ToString();

            url = url.Replace("{registrationId}", Uri.EscapeDataString(registrationId));
            url = url.Replace("{idScope}", Uri.EscapeDataString(idScope));

            var _queryParameters = new List<string>();
            if (forceRegistration != null)
            {
                _queryParameters.Add(string.Format(
                    CultureInfo.InvariantCulture,
                    "forceRegistration={0}",
                    Uri.EscapeDataString(SafeJsonConvert.SerializeObject(forceRegistration, Client.SerializationSettings).Trim('"'))));
            }
            if (_queryParameters.Count > 0)
            {
                url += "?" + string.Join("&", _queryParameters);
            }
            // Create HTTP transport objects
            var _httpRequest = new HttpRequestMessage();
            HttpResponseMessage _httpResponse = null;
            _httpRequest.Method = new HttpMethod("PUT");
            _httpRequest.RequestUri = new Uri(url);
            // Set Headers

            if (customHeaders != null)
            {
                foreach (KeyValuePair<string, List<string>> _header in customHeaders)
                {
                    if (_httpRequest.Headers.Contains(_header.Key))
                    {
                        _httpRequest.Headers.Remove(_header.Key);
                    }
                    _httpRequest.Headers.TryAddWithoutValidation(_header.Key, _header.Value);
                }
            }

            // Serialize Request
            string _requestContent = null;
            if (deviceRegistration != null)
            {
                _requestContent = SafeJsonConvert.SerializeObject(deviceRegistration, Client.SerializationSettings);
                _httpRequest.Content = new StringContent(_requestContent, Encoding.UTF8);
                _httpRequest.Content.Headers.ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8");
            }
            // Set Credentials
            if (Client.Credentials != null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Client.Credentials.ProcessHttpRequestAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            }
            // Send Request
            if (_shouldTrace)
            {
                ServiceClientTracing.SendRequest(_invocationId, _httpRequest);
            }
            cancellationToken.ThrowIfCancellationRequested();
            _httpResponse = await Client.HttpClient.SendAsync(_httpRequest, cancellationToken).ConfigureAwait(false);
            if (_shouldTrace)
            {
                ServiceClientTracing.ReceiveResponse(_invocationId, _httpResponse);
            }
            HttpStatusCode _statusCode = _httpResponse.StatusCode;
            cancellationToken.ThrowIfCancellationRequested();
            string _responseContent = null;
            if ((int)_statusCode != 202)
            {
                var ex = new HttpOperationException(string.Format(CultureInfo.InvariantCulture, "Operation returned an invalid status code '{0}'", _statusCode));
                if (_httpResponse.Content != null)
                {
                    _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                }
                else
                {
                    _responseContent = string.Empty;
                }
                ex.Request = new HttpRequestMessageWrapper(_httpRequest, _requestContent);
                ex.Response = new HttpResponseMessageWrapper(_httpResponse, _responseContent);
                if (_shouldTrace)
                {
                    ServiceClientTracing.Error(_invocationId, ex);
                }
                _httpRequest.Dispose();
                if (_httpResponse != null)
                {
                    _httpResponse.Dispose();
                }
                throw ex;
            }
            // Create Result
            var _result = new HttpOperationResponse<RegistrationOperationStatus>
            {
                Request = _httpRequest,
                Response = _httpResponse
            };
            // Deserialize Response
            if ((int)_statusCode == 202)
            {
                _responseContent = await _httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
                try
                {
                    _result.Body = SafeJsonConvert.DeserializeObject<RegistrationOperationStatus>(_responseContent, Client.DeserializationSettings);
                }
                catch (JsonException ex)
                {
                    _httpRequest.Dispose();
                    if (_httpResponse != null)
                    {
                        _httpResponse.Dispose();
                    }
                    throw new SerializationException("Unable to deserialize the response.", _responseContent, ex);
                }
            }
            if (_shouldTrace)
            {
                ServiceClientTracing.Exit(_invocationId, _result);
            }
            return _result;
        }
    }
}
