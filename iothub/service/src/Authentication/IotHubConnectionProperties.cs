// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The properties required for authentication to IoT hub that are independent of the authentication type.
    /// </summary>
    internal abstract class IotHubConnectionProperties
        : IAuthorizationHeaderProvider, ICbsTokenProvider
    {
        private const string HostNameSeparator = ".";
        private const string HttpsEndpointPrefix = "https";

        protected IotHubConnectionProperties(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException(nameof(hostName));
            }

            HostName = hostName;
            IotHubName = GetIotHubName(hostName);
            AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, HostName, AmqpConstants.DefaultSecurePort).Uri;
            HttpsEndpoint = new UriBuilder(HttpsEndpointPrefix, HostName).Uri;
        }

        public string IotHubName { get; protected set; }

        public string HostName { get; protected set; }

        public Uri HttpsEndpoint { get; protected set; }

        public Uri AmqpEndpoint { get; protected set; }

        public List<string> AmqpAudience { get; protected set; } = new List<string>();

        public abstract string GetAuthorizationHeader();

        public abstract Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims);

        public Uri BuildLinkAddress(string path)
        {
            var builder = new UriBuilder(AmqpEndpoint)
            {
                Path = path,
            };

            return builder.Uri;
        }

        internal static string GetIotHubName(string hostName)
        {
            if (string.IsNullOrWhiteSpace(hostName))
            {
                throw new ArgumentNullException($"{nameof(hostName)} is null or empty.");
            }

            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0 ? hostName.Substring(0, index) : hostName;
            return iotHubName;
        }
    }
}
