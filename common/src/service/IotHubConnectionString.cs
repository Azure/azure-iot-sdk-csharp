// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.Azure.Devices.Common;
using Microsoft.Azure.Devices.Common.Security;

namespace Microsoft.Azure.Devices
{
    internal sealed class IotHubConnectionString : IAuthorizationHeaderProvider, ICbsTokenProvider
    {
        private static readonly TimeSpan s_defaultTokenTimeToLive = TimeSpan.FromHours(1);
        private const string UserSeparator = "@";

        public IotHubConnectionString(IotHubConnectionStringBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Audience = builder.HostName;
            HostName = string.IsNullOrEmpty(builder.GatewayHostName) ? builder.HostName : builder.GatewayHostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            SharedAccessSignature = builder.SharedAccessSignature;
            IotHubName = builder.IotHubName;
            HttpsEndpoint = new UriBuilder("https", HostName).Uri;
            AmqpEndpoint = new UriBuilder(CommonConstants.AmqpsScheme, builder.HostName, AmqpConstants.DefaultSecurePort).Uri;
            DeviceId = builder.DeviceId;
            ModuleId = builder.ModuleId;
            GatewayHostName = builder.GatewayHostName;
        }

        public string IotHubName { get; private set; }

        public string HostName { get; private set; }

        public Uri HttpsEndpoint { get; private set; }

        public Uri AmqpEndpoint { get; private set; }

        public string Audience { get; private set; }

        public string SharedAccessKeyName { get; private set; }

        public string SharedAccessKey { get; private set; }

        public string SharedAccessSignature { get; private set; }

        public string DeviceId { get; private set; }

        public string ModuleId { get; private set; }

        public string GatewayHostName { get; private set; }

        public string GetUser()
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.Append(SharedAccessKeyName);
            stringBuilder.Append(UserSeparator);
            stringBuilder.Append("sas.");
            stringBuilder.Append("root.");
            stringBuilder.Append(IotHubName);

            return stringBuilder.ToString();
        }

        public string GetPassword()
        {
            string password;
            if (string.IsNullOrWhiteSpace(SharedAccessSignature))
            {
                password = BuildToken(out TimeSpan timeToLive);
            }
            else
            {
                password = SharedAccessSignature;
            }

            return password;
        }

        public string GetAuthorizationHeader()
        {
            return GetPassword();
        }

        Task<CbsToken> ICbsTokenProvider.GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
        {
            string tokenValue;
            CbsToken token;
            if (string.IsNullOrWhiteSpace(SharedAccessSignature))
            {
                tokenValue = BuildToken(out TimeSpan timeToLive);
                token = new CbsToken(tokenValue, CbsConstants.IotHubSasTokenType, DateTime.UtcNow.Add(timeToLive));
            }
            else
            {
                tokenValue = SharedAccessSignature;
                token = new CbsToken(tokenValue, CbsConstants.IotHubSasTokenType, DateTime.MaxValue);
            }

            return Task.FromResult(token);
        }

        public Uri BuildLinkAddress(string path)
        {
            var builder = new UriBuilder(AmqpEndpoint)
            {
                Path = path,
            };

            return builder.Uri;
        }

        public static IotHubConnectionString Parse(string connectionString)
        {
            var builder = IotHubConnectionStringBuilder.Create(connectionString);
            return new IotHubConnectionString(builder);
        }

        private string BuildToken(out TimeSpan ttl)
        {
            var builder = new SharedAccessSignatureBuilder
            {
                KeyName = SharedAccessKeyName,
                Key = SharedAccessKey,
                TimeToLive = s_defaultTokenTimeToLive,
                Target = Audience
            };

            if (DeviceId != null)
            {
                builder.Target = string.IsNullOrEmpty(ModuleId)
                    ? "{0}/devices/{1}".FormatInvariant(Audience, WebUtility.UrlEncode(DeviceId))
                    : "{0}/devices/{1}/modules/{2}".FormatInvariant(Audience, WebUtility.UrlEncode(DeviceId), WebUtility.UrlEncode(ModuleId));
            }

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }
    }
}
