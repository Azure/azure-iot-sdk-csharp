// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// This object handles the connection string for the Azure IoT Services.
    /// </summary>
    /// <remarks>
    /// The connection string contains a set of information that uniquely identify an IoT Service.
    ///
    /// A valid connection string shall be in the following format:
    /// <code>
    /// HostName=[ServiceName];SharedAccessKeyName=[keyName];SharedAccessKey=[Key]
    /// </code>
    ///
    /// This object parse and store the connection string. It is responsible to provide the authorization token too.
    /// </remarks>
    internal sealed class ServiceConnectionString : IAuthorizationHeaderProvider
    {
        private static readonly TimeSpan s_defaultTokenTimeToLive = TimeSpan.FromHours(1);

        internal ServiceConnectionString(ServiceConnectionStringBuilder builder)
        {
            Debug.Assert(builder != null, $"{nameof(builder)} cannot be null.");
            HostName = builder.HostName;
            SharedAccessKeyName = builder.SharedAccessKeyName;
            SharedAccessKey = builder.SharedAccessKey;
            SharedAccessSignature = builder.SharedAccessSignature;
            ServiceName = builder.ServiceName;
            HttpsEndpoint = new UriBuilder("https", builder.HostName).Uri;
        }

        public string ServiceName { get; private set; }

        public string HostName { get; private set; }

        public Uri HttpsEndpoint { get; private set; }

        public string Audience => HostName;

        public string SharedAccessKeyName { get; private set; }

        public string SharedAccessKey { get; private set; }

        public string SharedAccessSignature { get; private set; }

        public string GetPassword()
        {
            string password = string.IsNullOrWhiteSpace(SharedAccessSignature)
                ? BuildToken(out TimeSpan _)
                : SharedAccessSignature;
            return password;
        }

        public string GetAuthorizationHeader()
        {
            return GetPassword();
        }

        internal static ServiceConnectionString Parse(string connectionString)
        {
            var builder = ServiceConnectionStringBuilder.Create(connectionString);
            return new ServiceConnectionString(builder);
        }

        private string BuildToken(out TimeSpan ttl)
        {
            var builder = new SharedAccessSignatureBuilder
            {
                KeyName = SharedAccessKeyName,
                Key = SharedAccessKey,
                TimeToLive = s_defaultTokenTimeToLive,
                Target = Audience,
            };

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }
    }
}
