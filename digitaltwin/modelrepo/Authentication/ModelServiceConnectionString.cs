// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Azure.Devices.Common.Authorization;

namespace Microsoft.Azure.DigitalTwin.Model.Service
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
    public class ModelServiceConnectionString : ServiceConnectionString
    {
        /// <summary>
        /// Creates an instance based on a supplied parser
        /// </summary>
        /// <param name="parser">the <see cref="ModelServiceConnectionStringParser"/> with the connection string content.</param>
        /// <exception cref="ArgumentNullException">if the provided parser is null.</exception>
        public ModelServiceConnectionString(ModelServiceConnectionStringParser parser)
            : base(parser)
        {
            if (parser == null)
            {
                throw new ArgumentNullException(nameof(parser));
            }

            RepositoryId = parser.RespositoryId;
        }

        /// <summary>
        /// The Repository Id for private/company repository
        /// </summary>
        public string RepositoryId { get; }

        public override string BuildToken(out TimeSpan ttl)
        {
            var builder = new ModelSharedAccessSignatureBuilder
            {
                KeyName = SharedAccessKeyName,
                Key = SharedAccessKey,
                TimeToLive = DefaultTokenTimeToLive,
                hostName = Audience,
                ModelRepositoryId = RepositoryId,
            };

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }
    }
}
