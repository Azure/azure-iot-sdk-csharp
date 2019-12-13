using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoT.DigitalTwin.Service
{
    class AuthorizationDelegatingHandler : DelegatingHandler
    {
        IoTServiceClientCredentials ioTServiceClientCredentials;
        public AuthorizationDelegatingHandler(IoTServiceClientCredentials ioTServiceClientCredentials)
        {
            this.ioTServiceClientCredentials = ioTServiceClientCredentials;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await ioTServiceClientCredentials.ProcessHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}
