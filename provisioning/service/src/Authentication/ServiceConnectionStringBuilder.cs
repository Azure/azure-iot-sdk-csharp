// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text.RegularExpressions;
using SharedAccessSignatureParser = Microsoft.Azure.Devices.SharedAccessSignature;

namespace Microsoft.Azure.Devices
{
    internal class ServiceConnectionStringBuilder
    {
        private const char ValuePairDelimiter = ';';
        private const char ValuePairSeparator = '=';
        private const string HostNameSeparator = ".";

        private static readonly string s_hostNamePropertyName = ((MemberExpression)((Expression<Func<ServiceConnectionStringBuilder, string>>)(_ => _.HostName)).Body).Member.Name; // todo: replace with nameof()
        private static readonly string s_sharedAccessKeyNamePropertyName = ((MemberExpression)((Expression<Func<ServiceConnectionStringBuilder, string>>)(_ => _.SharedAccessKeyName)).Body).Member.Name; // todo: replace with nameof()
        private static readonly string s_sharedAccessKeyPropertyName = ((MemberExpression)((Expression<Func<ServiceConnectionStringBuilder, string>>)(_ => _.SharedAccessKey)).Body).Member.Name; // todo: replace with nameof()
        private static readonly string s_sharedAccessSignaturePropertyName = ((MemberExpression)((Expression<Func<ServiceConnectionStringBuilder, string>>)(_ => _.SharedAccessSignature)).Body).Member.Name; // todo: replace with nameof();

        private static readonly TimeSpan s_regexTimeoutMilliseconds = TimeSpan.FromMilliseconds(500);
        private static readonly Regex s_hostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessKeyRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);
        private static readonly Regex s_sharedAccessSignatureRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase, s_regexTimeoutMilliseconds);

        private string _hostName;
        private IAuthenticationMethod _authenticationMethod;

        private ServiceConnectionStringBuilder()
        {
        }

        public string HostName
        {
            get => _hostName;
            set => SetHostName(value);
        }

        public IAuthenticationMethod AuthenticationMethod
        {
            get => _authenticationMethod;
            set => SetAuthenticationMethod(value);
        }

        public string SharedAccessKeyName { get; internal set; }

        public string SharedAccessKey { get; internal set; }

        public string SharedAccessSignature { get; internal set; }

        public string ServiceName { get; private set; }

        internal static ServiceConnectionStringBuilder Create(string serviceConnectionString)
        {
            var serviceConnectionStringBuilder = new ServiceConnectionStringBuilder();
            serviceConnectionStringBuilder.Parse(serviceConnectionString);
            serviceConnectionStringBuilder.AuthenticationMethod = AuthenticationMethodFactory
                .GetAuthenticationMethod(serviceConnectionStringBuilder);

            return serviceConnectionStringBuilder;
        }

        private void Parse(string serviceConnectionString)
        {
            IDictionary<string, string> map = ToDictionary(serviceConnectionString, ValuePairDelimiter, ValuePairSeparator);

            HostName = GetConnectionStringValue(map, s_hostNamePropertyName);
            SharedAccessKeyName = GetConnectionStringOptionalValue(map, s_sharedAccessKeyNamePropertyName);
            SharedAccessKey = GetConnectionStringOptionalValue(map, s_sharedAccessKeyPropertyName);
            SharedAccessSignature = GetConnectionStringOptionalValue(map, s_sharedAccessSignaturePropertyName);

            Validate();
        }

        private void Validate()
        {
            if (string.IsNullOrWhiteSpace(SharedAccessKeyName))
            {
                throw new InvalidOperationException("Should specify SharedAccessKeyName");
            }

            if (!(string.IsNullOrWhiteSpace(SharedAccessKey) ^ string.IsNullOrWhiteSpace(SharedAccessSignature)))
            {
                throw new InvalidOperationException("Should specify either SharedAccessKey or SharedAccessSignature");
            }

            if (!string.IsNullOrWhiteSpace(SharedAccessKey))
            {
                Convert.FromBase64String(SharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(SharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(ServiceName, SharedAccessSignature);
            }

            ValidateFormat(HostName, s_hostNamePropertyName, s_hostNameRegex);
            ValidateFormatIfSpecified(SharedAccessKeyName, s_sharedAccessKeyNamePropertyName, s_sharedAccessKeyNameRegex);
            ValidateFormatIfSpecified(SharedAccessKey, s_sharedAccessKeyPropertyName, s_sharedAccessKeyRegex);
            ValidateFormatIfSpecified(SharedAccessSignature, s_sharedAccessSignaturePropertyName, s_sharedAccessSignatureRegex);
        }

        private void SetHostName(string hostname)
        {
            ValidateFormat(hostname, s_hostNamePropertyName, s_hostNameRegex);
            _hostName = hostname;
            SetServiceName();
        }

        private void SetServiceName()
        {
            ServiceName = GetServiceName(HostName);

            if (string.IsNullOrWhiteSpace(ServiceName))
            {
                throw new FormatException("Missing service name value from host name.");
            }
        }

        private void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            authMethod.Populate(this);
            _authenticationMethod = authMethod;
            Validate();
        }

        private static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new InvalidOperationException(
                    $"The connection string has an invalid value for property {propertyName}: {value}");
            }
        }

        private static void ValidateFormatIfSpecified(string value, string propertyName, Regex regex)
        {
            if (!string.IsNullOrEmpty(value))
            {
                ValidateFormat(value, propertyName, regex);
            }
        }

        private static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            if (!map.TryGetValue(propertyName, out string value))
            {
                throw new InvalidOperationException(
                    $"The connection string is missing the property: {propertyName}");
            }

            return value;
        }

        private static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            map.TryGetValue(propertyName, out string value);
            return value;
        }

        private static string GetServiceName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string serviceName = index >= 0 ? hostName.Substring(0, index) : hostName;
            return serviceName;
        }

        private static IDictionary<string, string> ToDictionary(string valuePairString, char kvpDelimiter, char kvpSeparator)
        {
            if (string.IsNullOrWhiteSpace(valuePairString))
            {
                throw new ArgumentException("Malformed Token");
            }

            IEnumerable<string[]> parts = valuePairString
                .Split(kvpDelimiter)
                .Select((part) => part.Split(new char[] { kvpSeparator }, 2));

            if (parts.Any((part) => part.Length != 2))
            {
                throw new FormatException("Malformed Token");
            }

            IDictionary<string, string> map = parts.ToDictionary((kvp) => kvp[0], (kvp) => kvp[1], StringComparer.OrdinalIgnoreCase);

            return map;
        }
    }
}
