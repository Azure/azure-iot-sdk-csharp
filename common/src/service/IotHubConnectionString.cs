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
    /// <summary>
    /// The IoT hub connection properties and methods required for authenticating using connection strings.
    /// </summary>
    internal sealed class IotHubConnectionString : IotHubCredential
    {
        private const char UserSeparator = '@';

        public IotHubConnectionString(IotHubConnectionStringBuilder builder)
            : base(builder.HostName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Audience = builder.HostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            SharedAccessSignature = builder.SharedAccessSignature;
            DeviceId = builder.DeviceId;
            ModuleId = builder.ModuleId;
            GatewayHostName = builder.GatewayHostName;
        }

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
                password = BuildToken(out _);
            }
            else
            {
                password = SharedAccessSignature;
            }

            return password;
        }

        public override string GetAuthorizationHeader()
        {
            return GetPassword();
        }

        public override Task<CbsToken> GetTokenAsync(Uri namespaceAddress, string appliesTo, string[] requiredClaims)
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
                TimeToLive = _defaultTokenTimeToLive,
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
