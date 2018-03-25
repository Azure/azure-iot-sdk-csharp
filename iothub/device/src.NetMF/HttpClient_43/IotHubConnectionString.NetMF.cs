// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.Client
{
    using System;
    using System.Net;
    using Microsoft.Azure.Devices.Client.Extensions;

    internal sealed partial class IotHubConnectionString : IAuthorizationProvider
    {
        internal static readonly TimeSpan DefaultTokenTimeToLive = new TimeSpan(1, 0, 0);

        string IAuthorizationProvider.GetPassword()
        {
            string password;
            if (this.SharedAccessSignature.IsNullOrWhiteSpace())
            {
                TimeSpan timeToLive;
                password = this.BuildToken(out timeToLive);
            }
            else
            {
                password = this.SharedAccessSignature;
            }

            return password;
        }

        string BuildToken(out TimeSpan ttl)
        {
            var builder = new SharedAccessSignatureBuilder()
            {
             Key = this.SharedAccessKey,
             TimeToLive = DefaultTokenTimeToLive,
            };

            if (this.SharedAccessKeyName == null)
            {
             builder.Target = this.Audience + "/devices/" + WebUtility.UrlEncode(this.DeviceId);
            }
            else
            {
             builder.KeyName = this.SharedAccessKeyName;
             builder.Target = this.Audience;
            }

            ttl = builder.TimeToLive;

            return builder.ToSignature();
        }

        public static IotHubConnectionString Parse(string connectionString)
        {
            var builder = IotHubConnectionStringBuilder.Create(connectionString);
            return builder.ToIotHubConnectionString();
        }        

        public Uri BuildLinkAddress(string path)
        {
            throw new NotImplementedException();
        }
    }
}
