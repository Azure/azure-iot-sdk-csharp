// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Azure.DigitalTwin.Model.Service;

namespace Microsoft.Azure.Devices.Common.Authorization
{
    /// <summary>
    /// The Service Connection String Parser class
    /// </summary>
    public class ModelServiceConnectionStringParser : ServiceConnectionStringParser
    {

        private const string RepositoryIdPropertyName = nameof(RespositoryId);

        private static readonly Regex RepositoryIdRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, regexTimeoutMilliseconds);

        private string _hostName;
        private string _serviceName;

        /// <summary>
        /// The Repository Id for private/Company repository
        /// </summary>
        public string RespositoryId { get; internal set; }

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
                SharedAccessSignature.Parse(ServiceName, SharedAccessSignatureString);
            }

            ValidateFormat(HostName, HostNamePropertyName, HostNameRegex);
            ValidateFormatIfSpecified(SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignatureString, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
            ValidateFormatIfSpecified(RespositoryId,RepositoryIdPropertyName,RepositoryIdRegex);
        }
    }
}
