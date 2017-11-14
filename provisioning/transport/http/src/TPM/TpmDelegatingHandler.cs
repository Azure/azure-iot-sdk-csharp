// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    internal class TpmDelegatingHandler : DelegatingHandler
    {
        internal const string ProvisioningHeaderName = "drs-set-sas-token";
        private readonly SecurityClientHsmTpm _securityClient;

        public TpmDelegatingHandler(SecurityClientHsmTpm securityClient)
        {
            _securityClient = securityClient;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, 
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled) Logging.Enter(this, $"{request.RequestUri}", nameof(SendAsync));

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if(response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (request.Properties.TryGetValue(ProvisioningHeaderName, out object result))
                {
                    if (result is Action<string> setSasToken)
                    {
                        string target = GetTarget(request.RequestUri.LocalPath);
                        string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        TpmChallenge challenge = JsonConvert.DeserializeObject<TpmChallenge>(responseContent);

                        string sasToken = ProvisioningSasBuilder.ExtractServiceAuthKey(
                            _securityClient,
                            target, 
                            Convert.FromBase64String(challenge.AuthenticationKey));

                        setSasToken(sasToken);

                        if (Logging.IsEnabled) Logging.Info(
                            this, 
                            $"Authorization challenge. Retrying with Token:{sasToken}");

                        response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (Logging.IsEnabled) Logging.Exit(this, $"{request.RequestUri}", nameof(SendAsync));

            return response;
        }

        private string GetTarget(string requestUriLocalPath)
        {
            requestUriLocalPath = requestUriLocalPath.TrimStart('/');
            string[] parameters = requestUriLocalPath.Split('/');
            if (parameters.Length <= 3)
            {
                throw new ArgumentException($"Invalid RequestUri LocalPath");
            }

            return string.Concat(parameters[0], "/" , parameters[1], "/", parameters[2]);
        }
    }
}
