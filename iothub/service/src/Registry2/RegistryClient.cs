// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Microsoft.Azure.Devices.Common.Exceptions;
using Microsoft.Azure.Devices.Http2;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// Client class for all registry related operations such as
    /// adding/updating/getting/deleting device/module identities
    /// and getting registry statistics.
    /// </summary>
    public class RegistryClient : IDisposable
    {
        private string _hostName;
        private IotHubConnectionProperties _credentialProvider;
        private HttpClient _httpClient;
        private HttpRequestMessageFactory _httpRequestMessageFactory;

        /// <summary>
        /// Creates an instance of this class. Provided for unit testing purposes only.
        /// </summary>
        protected RegistryClient()
        {
        }

        internal RegistryClient(string hostName, IotHubConnectionProperties credentialProvider, HttpClient httpClient, HttpRequestMessageFactory httpRequestMessageFactory)
        {
            _credentialProvider = credentialProvider;
            _hostName = hostName;
            _httpClient = httpClient;
            _httpRequestMessageFactory = httpRequestMessageFactory;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }
    }
}
