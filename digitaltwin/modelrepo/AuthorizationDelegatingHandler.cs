using Microsoft.Azure.Devices.Common.Authorization;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    internal class AuthorizationDelegatingHandler : DelegatingHandler
    {
        private readonly IoTServiceClientCredentials _iotServiceClientCredentials;

        public AuthorizationDelegatingHandler(IoTServiceClientCredentials ioTServiceClientCredentials)
        {
            _iotServiceClientCredentials = ioTServiceClientCredentials;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await _iotServiceClientCredentials.ProcessHttpRequestAsync(request, cancellationToken).ConfigureAwait(false);
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }
    }
}