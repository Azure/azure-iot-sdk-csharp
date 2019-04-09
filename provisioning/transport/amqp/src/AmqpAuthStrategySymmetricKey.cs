// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Amqp;
using Microsoft.Azure.Amqp.Sasl;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Azure.Devices.Provisioning.Client.Transport.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class AmqpAuthStrategySymmetricKey : AmqpAuthStrategy
    {
        private SecurityProviderSymmetricKey _security;

        public AmqpAuthStrategySymmetricKey(SecurityProviderSymmetricKey security)
        {
            _security = security;
        }

        public override AmqpSettings CreateAmqpSettings(string idScope)
        {
            var settings = new AmqpSettings();

            var saslProvider = new SaslTransportProvider();
            saslProvider.Versions.Add(AmqpConstants.DefaultProtocolVersion);
            settings.TransportProviders.Add(saslProvider);

            SaslPlainHandler saslHandler = new SaslPlainHandler();
            saslHandler.AuthenticationIdentity = $"{idScope}/registrations/{_security.GetRegistrationID()}";
            string key = _security.GetPrimaryKey();
            saslHandler.Password = BuildSasSignature("registration", key, saslHandler.AuthenticationIdentity, TimeSpan.FromDays(1));
            saslProvider.AddHandler(saslHandler);

            return settings;
        }

        public override Task OpenConnectionAsync(AmqpClientConnection connection, TimeSpan timeout, bool useWebSocket, IWebProxy proxy)
        {
            return connection.OpenAsync(timeout, useWebSocket, null, proxy);
        }

        public override void SaveCredentials(RegistrationOperationStatus operation)
        {
        }

        //TODO: merge with other Sas Token Builder in ProvisioningSasBuilder and in IotHubClient
        private static string BuildSasSignature(string keyName, string key, string target, TimeSpan timeToLive)
        {
            string expiresOn = ProvisioningSasBuilder.BuildExpiresOn(timeToLive);
            string audience = WebUtility.UrlEncode(target);
            var fields = new List<string>
            {
                audience,
                expiresOn
            };

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(CultureInfo.InvariantCulture, "{0} {1}={2}&{3}={4}&{5}={6}",
                "SharedAccessSignature",
                "sr", audience,
                "sig", WebUtility.UrlEncode(signature),
                "se", WebUtility.UrlEncode(expiresOn));

            if (!string.IsNullOrEmpty(keyName))
            {
                buffer.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}",
                    "skn", WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        private static string Sign(string requestString, string key)
        {
            using (var hmac = new HMACSHA256(Convert.FromBase64String(key)))
            {
                return Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(requestString)));
            }
        }
    }
}
