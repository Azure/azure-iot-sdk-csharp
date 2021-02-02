// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The IoT hub authentication properties that are not dependent on the authentication type.
    /// </summary>
    internal abstract class IotHubCredential : IAuthorizationHeaderProvider, ICbsTokenProvider
    {
        private const string HostNameSeparator = ".";

        protected static readonly TimeSpan _defaultTokenTimeToLive = TimeSpan.FromHours(1);

        public string IotHubName { get; protected set; }

        public string HostName { get; protected set; }

        public Uri HttpsEndpoint { get; protected set; }

        public Uri AmqpEndpoint { get; protected set; }

        public abstract string GetAuthorizationHeader();

        public abstract Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims);

        // For NET 451 support.
        protected IotHubCredential()
        {
        }

        protected IotHubCredential(string hostName)
        {
            HostName = hostName;
            IotHubName = GetIotHubName(hostName);
            AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, HostName, AmqpConstants.DefaultSecurePort).Uri;
            HttpsEndpoint = new UriBuilder("https", HostName).Uri;
        }

        public Uri BuildLinkAddress(string path)
        {
            var builder = new UriBuilder(AmqpEndpoint)
            {
                Path = path,
            };

            return builder.Uri;
        }

        private static string GetIotHubName(string hostName)
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
