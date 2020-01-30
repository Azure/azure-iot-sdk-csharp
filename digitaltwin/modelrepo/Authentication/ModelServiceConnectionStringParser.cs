// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.Devices.Common.Authorization;

namespace Microsoft.Azure.DigitalTwin.Model.Service
{
    /// <summary>
    /// The Model Service Connection String Parser class
    /// </summary>
    public class ModelServiceConnectionStringParser : ServiceConnectionStringParser
    {
        protected const string RepositoryIdPropertyName = "RepositoryId";

        private static readonly Regex RepositoryIdRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);

        /// <summary>
        /// The Repository Id for private/Company repository
        /// </summary>
        public string RespositoryId { get; internal set; }

        /// <summary>
        /// Factory for new Connection String object.
        /// </summary>
        /// <remarks>
        /// The connection string contains a set of information that uniquely identify an IoT Service.
        ///
        /// A valid connection string shall be in the following format:
        /// <code>
        /// HostName=[ServiceName];SharedAccessKeyName=[keyName];SharedAccessKey=[Key]
        /// </code>
        ///
        /// This object parse the connection string providing the artifacts to the <see cref="ModelServiceConnectionString"/> object.
        /// </remarks>
        /// <param name="modelServiceConnectionString">the <code>string</code> with the connection string information.</param>
        /// <returns>A <code>ModelServiceConnectionStringParser</code> object with the parsed connection string.</returns>
        public static ModelServiceConnectionStringParser CreateForModel(string modelServiceConnectionString)
        {
            if (string.IsNullOrWhiteSpace(modelServiceConnectionString))
            {
                throw new ArgumentNullException(nameof(modelServiceConnectionString));
            }

            var serviceConnectionStringParser = new ModelServiceConnectionStringParser();
           serviceConnectionStringParser.Parse(modelServiceConnectionString);

            return serviceConnectionStringParser;
        }

        /// <summary>
        /// Returns the Service Connection string
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Validate();

            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, HostName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, SharedAccessKeyName);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, SharedAccessSignatureString);
            stringBuilder.AppendKeyValuePairIfNotEmpty(RepositoryIdPropertyName, RespositoryId);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        protected override void Parse(string serviceConnectionString)
        {
            IDictionary<string, string> map = serviceConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, HostNamePropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            SharedAccessSignatureString = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            RespositoryId = GetConnectionStringOptionalValue(map, RepositoryIdPropertyName);
            Validate();
        }

        protected override void Validate()
        {
            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new FormatException("Missing service name");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
            {
                Convert.FromBase64String(SharedAccessKey);
            }

            if(!string.IsNullOrWhiteSpace(RespositoryId))
            {
                Convert.FromBase64String(RespositoryId);
            }

            if (SharedAccessSignature.IsSharedAccessSignature(SharedAccessSignatureString))
            {
                ModelSharedAccessSignature.ParseForModel(ServiceName, SharedAccessSignatureString);
            }

            ValidateFormat(HostName, HostNamePropertyName, HostNameRegex);
            ValidateFormatIfSpecified(SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignatureString, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
            ValidateFormatIfSpecified(RespositoryId,RepositoryIdPropertyName,RepositoryIdRegex);
        }
    }
}