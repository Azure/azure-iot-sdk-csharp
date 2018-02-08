// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq.Expressions;
    using System.Text;
    using System.Text.RegularExpressions;
    using Microsoft.Azure.Devices.Common;
    using SharedAccessSignatureParser = Microsoft.Azure.Devices.Common.Security.SharedAccessSignature;

    /// <summary>
    /// Builds a connection string for the IoT Hub service based on the properties populated by the user.
    /// </summary>
    public class IotHubConnectionStringBuilder
    {
        const char ValuePairDelimiter = ';';
        const char ValuePairSeparator = '=';
        const string HostNameSeparator = ".";

        static readonly string HostNamePropertyName = ((MemberExpression)((Expression<Func<IotHubConnectionStringBuilder, string>>)(_ => _.HostName)).Body).Member.Name; // todo: replace with nameof()
        static readonly string SharedAccessKeyNamePropertyName = ((MemberExpression)((Expression<Func<IotHubConnectionStringBuilder, string>>)(_ => _.SharedAccessKeyName)).Body).Member.Name; // todo: replace with nameof()
        static readonly string DeviceIdPropertyName = ((MemberExpression)((Expression<Func<IotHubConnectionStringBuilder, string>>)(_ => _.DeviceId)).Body).Member.Name; // todo: replace with nameof()
        static readonly string ModuleIdPropertyName = nameof(ModuleId);
        static readonly string GatewayHostNamePropertyName = nameof(GatewayHostName);
        static readonly string SharedAccessKeyPropertyName = ((MemberExpression)((Expression<Func<IotHubConnectionStringBuilder, string>>)(_ => _.SharedAccessKey)).Body).Member.Name; // todo: replace with nameof()
        static readonly string SharedAccessSignaturePropertyName = ((MemberExpression)((Expression<Func<IotHubConnectionStringBuilder, string>>)(_ => _.SharedAccessSignature)).Body).Member.Name; // todo: replace with nameof();
        static readonly Regex HostNameRegex = new Regex(@"[a-zA-Z0-9_\-\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex SharedAccessKeyNameRegex = new Regex(@"^[a-zA-Z0-9_\-@\.]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex SharedAccessKeyRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex SharedAccessSignatureRegex = new Regex(@"^.+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        static readonly Regex IdRegex = new Regex(@"^[A-Za-z0-9\-:.+%_#*?!(),=@;$']{1,128}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        string hostName;
        string iotHubName;
        IAuthenticationMethod authenticationMethod;

        IotHubConnectionStringBuilder()
        {
        }

        public static IotHubConnectionStringBuilder Create(string hostname, IAuthenticationMethod authenticationMethod)
        {
            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder
            {
                HostName = hostname,
                AuthenticationMethod = authenticationMethod
            };

            iotHubConnectionStringBuilder.Validate();

            return iotHubConnectionStringBuilder;
        }

        public static IotHubConnectionStringBuilder Create(string iotHubConnectionString)
        {
            if (string.IsNullOrWhiteSpace(iotHubConnectionString))
            {
                throw new ArgumentNullException("iotHubConnectionString");
            }

            var iotHubConnectionStringBuilder = new IotHubConnectionStringBuilder();        
            iotHubConnectionStringBuilder.Parse(iotHubConnectionString);
            iotHubConnectionStringBuilder.AuthenticationMethod = AuthenticationMethodFactory.GetAuthenticationMethod(iotHubConnectionStringBuilder);

            return iotHubConnectionStringBuilder;
        }

        public string HostName
        {
            get { return this.hostName; }
            set { this.SetHostName(value); }
        }

        public IAuthenticationMethod AuthenticationMethod
        {
            get { return this.authenticationMethod; }
            set { this.SetAuthenticationMethod(value); }
        }

        public string SharedAccessKeyName { get; internal set; }

        public string SharedAccessKey { get; internal set; }

        public string SharedAccessSignature { get; internal set; }

        public string DeviceId { get; internal set; }

        public string ModuleId { get; internal set; }

        public string GatewayHostName { get; internal set; }

        public string IotHubName
        {
            get { return this.iotHubName; }
        }

        internal IotHubConnectionString ToIotHubConnectionString()
        {
            this.Validate();
            return new IotHubConnectionString(this);
        }

        public override string ToString()
        {
            this.Validate();

            var stringBuilder = new StringBuilder();
            stringBuilder.AppendKeyValuePairIfNotEmpty(HostNamePropertyName, this.HostName);
            if (this.SharedAccessKeyName != null)
            {
                stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyNamePropertyName, this.SharedAccessKeyName);
            }            
            else
            {
                if (this.ModuleId != null)
                {
                    stringBuilder.AppendKeyValuePairIfNotEmpty(ModuleIdPropertyName, this.ModuleId);
                }

                stringBuilder.AppendKeyValuePairIfNotEmpty(DeviceIdPropertyName, this.DeviceId);
                stringBuilder.AppendKeyValuePairIfNotEmpty(GatewayHostNamePropertyName, this.GatewayHostName);
            }

            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessKeyPropertyName, this.SharedAccessKey);
            stringBuilder.AppendKeyValuePairIfNotEmpty(SharedAccessSignaturePropertyName, this.SharedAccessSignature);
            if (stringBuilder.Length > 0)
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);
            }

            return stringBuilder.ToString();
        }

        void Parse(string iotHubConnectionString)
        {
            IDictionary<string, string> map = iotHubConnectionString.ToDictionary(ValuePairDelimiter, ValuePairSeparator);

            this.HostName = GetConnectionStringValue(map, HostNamePropertyName);
            this.SharedAccessKeyName = GetConnectionStringOptionalValue(map, SharedAccessKeyNamePropertyName);
            this.SharedAccessKey = GetConnectionStringOptionalValue(map, SharedAccessKeyPropertyName);
            this.SharedAccessSignature = GetConnectionStringOptionalValue(map, SharedAccessSignaturePropertyName);
            this.DeviceId = GetConnectionStringOptionalValue(map, DeviceIdPropertyName);
            this.ModuleId = GetConnectionStringOptionalValue(map, ModuleIdPropertyName);
            this.GatewayHostName = GetConnectionStringOptionalValue(map, GatewayHostNamePropertyName);

            this.Validate();
        }

        void Validate()
        {
            if (this.SharedAccessKeyName.IsNullOrWhiteSpace() && this.DeviceId.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Should specify either SharedAccessKeyName or DeviceId");
            }

            if (!(this.SharedAccessKey.IsNullOrWhiteSpace() ^ this.SharedAccessSignature.IsNullOrWhiteSpace()))
            {
                throw new ArgumentException("Should specify either SharedAccessKey or SharedAccessSignature");
            }

            if (string.IsNullOrWhiteSpace(this.IotHubName))
            {
                throw new FormatException("Missing IOT hub name");
            }

            if (!this.SharedAccessKey.IsNullOrWhiteSpace())
            {
                Convert.FromBase64String(this.SharedAccessKey);
            }

            if (SharedAccessSignatureParser.IsSharedAccessSignature(this.SharedAccessSignature))
            {
                SharedAccessSignatureParser.Parse(this.IotHubName, this.SharedAccessSignature);
            }
            
            ValidateFormat(this.HostName, HostNamePropertyName, HostNameRegex);
            if (!this.SharedAccessKeyName.IsNullOrWhiteSpace())
            {
                ValidateFormatIfSpecified(this.SharedAccessKeyName, SharedAccessKeyNamePropertyName, SharedAccessKeyNameRegex);
            }
            if (!this.DeviceId.IsNullOrWhiteSpace())
            {
                ValidateFormatIfSpecified(this.DeviceId, DeviceIdPropertyName, IdRegex);
            }
            if (!this.ModuleId.IsNullOrWhiteSpace())
            {
                ValidateFormatIfSpecified(this.ModuleId, ModuleIdPropertyName, IdRegex);
            }
            ValidateFormatIfSpecified(this.SharedAccessKey, SharedAccessKeyPropertyName, SharedAccessKeyRegex);
            ValidateFormatIfSpecified(this.SharedAccessSignature, SharedAccessSignaturePropertyName, SharedAccessSignatureRegex);
            ValidateFormatIfSpecified(this.GatewayHostName, GatewayHostNamePropertyName, HostNameRegex);
        }

        void SetHostName(string hostname)
        {
            if (string.IsNullOrWhiteSpace(hostname))
            {
                throw new ArgumentNullException("hostname");
            }

            ValidateFormat(hostname, HostNamePropertyName, HostNameRegex);
            this.hostName = hostname;
            this.SetIotHubName();
        }

        void SetIotHubName()
        {
            this.iotHubName = GetIotHubName(this.HostName);

            if (string.IsNullOrWhiteSpace(this.IotHubName))
            {
                throw new FormatException("Missing IOT hub name");
            }
        }

        void SetAuthenticationMethod(IAuthenticationMethod authMethod)
        {
            if (authMethod == null)
            {
                throw new ArgumentNullException("authMethod");
            }

            authMethod.Populate(this);
            this.authenticationMethod = authMethod;
            this.Validate();
        }

        static void ValidateFormat(string value, string propertyName, Regex regex)
        {
            if (!regex.IsMatch(value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string has an invalid value for property: {0}", propertyName), "iotHubConnectionString");
            }
        }

        static void ValidateFormatIfSpecified(string value, string propertyName, Regex regex)
        {
            if (!string.IsNullOrEmpty(value))
            {
                ValidateFormat(value, propertyName, regex);
            }
        }

        static string GetConnectionStringValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            if (!map.TryGetValue(propertyName, out value))
            {
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The connection string is missing the property: {0}", propertyName), "iotHubConnectionString");
            }

            return value;
        }

        static string GetConnectionStringOptionalValue(IDictionary<string, string> map, string propertyName)
        {
            string value;
            map.TryGetValue(propertyName, out value);
            return value;
        }

        static string GetIotHubName(string hostName)
        {
            int index = hostName.IndexOf(HostNameSeparator, StringComparison.OrdinalIgnoreCase);
            string iotHubName = index >= 0 ? hostName.Substring(0, index) : null;
            return iotHubName;
        }
    }
}