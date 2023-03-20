// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Azure;
using Azure.Core;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// The local implementation of the Azure.Core Response type. Libraries in the azure-sdk-for-net repo have access to 
    /// helper functions to instantiate the abstract class Response, but this library is not in that repo yet. Because of that,
    /// we need to implement the abstract class.
    /// </summary>
    internal class QueryResponse : Response
    {
        private HttpResponseMessage _httpResponse;
        private Stream _bodyStream;
        private List<HttpHeader> _httpHeaders;

        internal QueryResponse(HttpResponseMessage httpResponse, Stream bodyStream) 
        { 
            _httpResponse = httpResponse;
            _bodyStream = bodyStream;

            _httpHeaders = new List<HttpHeader>();
            foreach (var header in _httpResponse.Headers)
            {
                _httpHeaders.Add(new HttpHeader(header.Key, header.Value.First()));
            }
        }

        public override int Status => (int)_httpResponse.StatusCode;

        public override string ReasonPhrase => _httpResponse.ReasonPhrase;

        public override Stream ContentStream 
        {
            get =>  _bodyStream;
            set => _bodyStream = value;
        }

        public override string ClientRequestId 
        { 
            get => throw new NotImplementedException("This SDK does not define this feature"); 
            set => throw new NotImplementedException("This SDK does not define this feature"); 
        }

        public override void Dispose()
        {
            _httpResponse?.Dispose();
            _bodyStream?.Dispose();
        }

        protected override bool ContainsHeader(string name)
        {
            Argument.AssertNotNullOrWhiteSpace(name, nameof(name));
            return _httpResponse.Headers.Contains(name);
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            return _httpHeaders;
        }

        protected override bool TryGetHeader(string name, out string value)
        {
            Argument.AssertNotNullOrWhiteSpace(name, nameof(name));
            value = _httpResponse.Headers.SafeGetValue(name);
            return string.IsNullOrWhiteSpace(value);
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            Argument.AssertNotNullOrWhiteSpace(name, nameof(name));
            return _httpResponse.Headers.TryGetValues(name, out values);
        }
    }
}
