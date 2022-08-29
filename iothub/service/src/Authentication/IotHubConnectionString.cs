// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// The properties required for authentication to IoT hub using a connection string.
    /// </summary>
    internal sealed class IotHubConnectionString : IotHubConnectionProperties
    {
        private static readonly TimeSpan s_tokenTimeToLive = TimeSpan.FromHours(1);

        public IotHubConnectionString(IotHubConnectionStringBuilder builder) : base(builder?.HostName)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            Audience = builder.HostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            SharedAccessSignature = builder.SharedAccessSignature;
        }

        public string Audience { get; private set; }

        public string SharedAccessKeyName { get; private set; }

        public string SharedAccessKey { get; private set; }

        public string SharedAccessSignature { get; private set; }

        public string GetPassword()
        {
            return string.IsNullOrWhiteSpace(SharedAccessSignature)
                ? BuildToken(out _)
                : SharedAccessSignature;
        }

        public override string GetAuthorizationHeader() => GetPassword();

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
                TimeToLive = s_tokenTimeToLive,
                Target = Audience
            };

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }
    }
}
