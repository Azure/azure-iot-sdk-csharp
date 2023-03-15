// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using Azure;
using Azure.Core;

namespace Microsoft.Azure.Devices
{
    internal class QueryResponse : Response
    {
        private HttpResponseMessage _httpResponse;
        private List<HttpHeader> _httpHeaders;

        internal QueryResponse(HttpResponseMessage httpResponse) 
        { 
            _httpResponse = httpResponse;

            _httpHeaders = new List<HttpHeader>();
            foreach (var header in _httpResponse.Headers)
            {
                _httpHeaders.Add(new HttpHeader(header.Key, header.Value.First()));
            }
        }

        public override int Status => (int)_httpResponse.StatusCode; //TODO check this

        public override string ReasonPhrase => _httpResponse.ReasonPhrase;

        public override Stream ContentStream 
        {
            get =>  _httpResponse.Content.ReadAsStreamAsync().Result; 
            set => throw new NotImplementedException(); //TODO who needs this?
        }
        public override string ClientRequestId 
        { 
            get => throw new NotImplementedException(); 
            set => throw new NotImplementedException(); 
        }

        public override void Dispose()
        {
            _httpResponse?.Dispose();
        }

        protected override bool ContainsHeader(string name)
        {
            return _httpResponse.Headers.Contains(name);
        }

        protected override IEnumerable<HttpHeader> EnumerateHeaders()
        {
            return _httpHeaders;
        }

        protected override bool TryGetHeader(string name, out string value)
        {
            IEnumerable<string> outVariableHeaders = new List<string>();
            value = _httpResponse.Headers.TryGetValues(name, out outVariableHeaders)
                ? outVariableHeaders.First()
                : string.Empty;
            if (found)
            {
                value = outVariableHeaders.First();
            }
            else
            {
                value = "";
            }

            return found;
        }

        protected override bool TryGetHeaderValues(string name, out IEnumerable<string> values)
        {
            return _httpResponse.Headers.TryGetValues(name, out values);
        }
    }
}
