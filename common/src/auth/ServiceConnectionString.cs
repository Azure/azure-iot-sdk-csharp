// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    /// <summary>
    /// This object handles the deconstruction a connection string into the various key/value pairs for the Azure IoT Services.
    /// </summary>
    /// <remarks>
    /// The connection string contains a set of information that uniquely identifies an IoT service.
    ///
    /// A valid connection string shall be in one of the following formats:
    /// <code>
    /// HostName=[repositoryHostName];RepositoryId=[repositoryId];SharedAccessKeyName=[keyName];SharedAccessKey=[keyValue]
    /// HostName=[repositoryHostName];SharedAccessKeyName=[keyName];SharedAccessSignature=[signature]
    /// </code>
    ///
    /// This object parses and stores the connection string. It is responsible to provide the authorization token too.
    /// </remarks>
    public class ServiceConnectionString
    {
        protected static readonly TimeSpan DefaultTokenTimeToLive = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Creates an instance based on a supplied parser
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
            SharedAccessSignature = parser.SharedAccessSignatureString;
            ServiceName = parser.ServiceName;
            HttpsEndpoint = new UriBuilder("https", parser.HostName).Uri;
        }

        /// <summary>
        /// Parser for the Service Connection String
        /// </summary>
        /// <param name="connectionString"> The Connection String </param>
        public static ServiceConnectionString Parse(string connectionString)
        {
            var parser = ServiceConnectionStringParser.Create(connectionString);
            return new ServiceConnectionString(parser);
        }

        /// <summary>
        /// The Service Name
        /// </summary>
        public string ServiceName { get; }

        /// <summary>
        /// The Service Client Hostname
        /// </summary>
        public string HostName { get; }

        /// <summary>
        /// The Service Client Https Endpoint
        /// </summary>
        public Uri HttpsEndpoint { get; }

        /// <summary>
        /// The Service Audience
        /// </summary>
        public string Audience => HostName;

        /// <summary>
        /// The Service Access Key Name
        /// </summary>
        public string SharedAccessKeyName { get; }

        /// <summary>
        /// The Service Shared Access Key for the specified access policy
        /// </summary>
        public string SharedAccessKey { get; }

        /// <summary>
        /// The Service Shared Access Signature
        /// </summary>
        public string SharedAccessSignature { get; }

        /// <summary>
        /// Returns the shared access signature for authorization
        /// </summary>
        public string GetSasToken()
        {
            return string.IsNullOrWhiteSpace(SharedAccessSignature) 
                ? BuildToken(out TimeSpan timeToLive) 
                : SharedAccessSignature;
        }

        public virtual string BuildToken(out TimeSpan ttl)
        {
            var builder = new SharedAccessSignatureBuilder
            {
                KeyName = SharedAccessKeyName,
                Key = SharedAccessKey,
                TimeToLive = DefaultTokenTimeToLive,
                hostName = Audience,
            };

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }
    }
}
