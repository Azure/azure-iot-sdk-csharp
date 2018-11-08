// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.Azure.Devices.Provisioning.Service
{
    /// <summary>
    /// This object handles the connection string for the Azure IoT Services.
    /// </summary>
    /// <remarks>
    /// The connection string contains a set of information that uniquely identify an IoT Service.
    /// 
    /// A valid connection string shall be in one of the following formats:
    /// <code>
    /// HostName=[ServiceName];SharedAccessKeyName=[keyName];SharedAccessKey=[Key]
    /// HostName=[ServiceName];SharedAccessKeyName=[keyName];SharedAccessSignature=[Signature]
    /// </code>
    /// 
    /// This object parse and store the connection string. It is responsible to provide the authorization token too. 
    /// </remarks>
    public sealed class ServiceConnectionString
    {
        private static readonly TimeSpan DefaultTokenTimeToLive = TimeSpan.FromMinutes(5);

        /// <summary>
        /// CONSTRUCOR
        /// </summary>
        /// <param name="parser">the <see cref="ServiceConnectionStringParser"/> with the connection string content.</param>
        /// <exception cref="ArgumentNullException">if the provided parser is null.</exception>
        public ServiceConnectionString(ServiceConnectionStringParser parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            HostName = parser.HostName;
            SharedAccessKeyName = parser.SharedAccessKeyName;
            SharedAccessKey = parser.SharedAccessKey;
            SharedAccessSignature = parser.SharedAccessSignature;
            ServiceName = parser.ServiceName;
            HttpsEndpoint = new UriBuilder("https", parser.HostName).Uri;
        }

        /// <summary>
        /// The Provisioning Service Name
        /// </summary>
        public string ServiceName
        {
            get;
            private set;
        }

        /// <summary>
        /// The Provisioning Service Client Hostname
        /// </summary>
        public string HostName
        {
            get;
            private set;
        }

        /// <summary>
        /// The Provisioning Service Client Https Endpoint
        /// </summary>
        public Uri HttpsEndpoint
        {
            get;
            private set;
        }

        /// <summary>
        /// The Provisioning Service Audience
        /// </summary>
        public string Audience
        {
            get { return HostName; }
        }

        /// <summary>
        /// The Provisioning Service Access Policy Name
        /// </summary>
        public string SharedAccessKeyName
        {
            get;
            private set;
        }

        /// <summary>
        /// The Provisioning Service Shared Access Key for the specified
        /// access policy
        /// </summary>
        public string SharedAccessKey
        {
            get;
            private set;
        }

        /// <summary>
        /// The Provisioning Service Shared Access Signature
        /// </summary>
        public string SharedAccessSignature
        {
            get;
            private set;
        }

        /// <summary>
        /// Returns the shared access signature for authorization
        /// </summary>
        /// <returns></returns>
        public string GetSasToken()
        {
            string password;
            if (string.IsNullOrWhiteSpace(SharedAccessSignature))
            {
                TimeSpan timeToLive;
                password = BuildToken(out timeToLive);
            }
            else
            {
                password = SharedAccessSignature;
            }

            return password;
        }

        /// <summary>
        /// Parser for the Provisioning Service Connection String
        /// </summary>
        /// <param name="connectionString"> The DPS Connection String </param>
        /// <returns></returns>
        public static ServiceConnectionString Parse(string connectionString)
        {
            var parser = ServiceConnectionStringParser.Create(connectionString);
            return new ServiceConnectionString(parser);
        }

        private string BuildToken(out TimeSpan ttl)
        {
            var builder = new SharedAccessSignatureBuilder()
            {
                KeyName = SharedAccessKeyName,
                Key = SharedAccessKey,
                TimeToLive = DefaultTokenTimeToLive,
                Target = Audience
            };

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }
    }
}
