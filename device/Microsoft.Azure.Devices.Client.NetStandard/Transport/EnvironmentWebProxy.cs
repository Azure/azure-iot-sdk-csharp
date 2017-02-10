using System;

namespace Microsoft.Azure.Devices.Client.Transport
{
    using System.Net;

    sealed class EnvironmentWebProxy : IWebProxy
    {
        readonly Uri proxy;

        public EnvironmentWebProxy(Uri proxy)
        {
            this.proxy = proxy;
            this.Credentials =
                new NetworkCredential(
                    Environment.GetEnvironmentVariable("PROXY_USERNAME"),
                    Environment.GetEnvironmentVariable("PROXY_PASSWORD")
                );
        }

        public Uri GetProxy(Uri destination) => this.proxy;

        public bool IsBypassed(Uri host) => host.IsLoopback;

        public ICredentials Credentials { get; set; }
    }
}