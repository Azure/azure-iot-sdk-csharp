using Microsoft.Azure.Devices.Provisioning.Service;
using Microsoft.Rest;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Common.Service.Auth
{
    /// <summary>
    /// Shared Access Key Signature class.
    /// </summary>
    public class SharedAccessKeyCredentials : ServiceClientCredentials
    {
        private static ServiceConnectionString _provisioningConnectionString;
        private ProductInfo _productInfo = new ProductInfo();

        /// <summary>
        /// Create a new instance of <code>SharedAccessKeyCredentials</code> using
        /// the Shared Access Key
        /// </summary>
        public SharedAccessKeyCredentials(ServiceConnectionString serviceConnectionString)
        {
            _provisioningConnectionString = serviceConnectionString;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="request"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.Headers.Add(HttpRequestHeader.Authorization.ToString(), _provisioningConnectionString.GetAuthorizationHeader());
            request.Headers.Add("User-Agent", _productInfo.ToString());
            return base.ProcessHttpRequestAsync(request, cancellationToken);
        }
    }
}
