using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    internal class AuthorizationDelegatingHandler : DelegatingHandler
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