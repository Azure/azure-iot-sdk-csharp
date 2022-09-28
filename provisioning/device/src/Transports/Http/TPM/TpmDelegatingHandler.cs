// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Authentication;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    internal class TpmDelegatingHandler : DelegatingHandler
    {
        internal const string ProvisioningHeaderName = "drs-set-sas-token";
        private readonly AuthenticationProviderTpm _authProvider;

        public TpmDelegatingHandler(AuthenticationProviderTpm authenticationProvider)
        {
            _authProvider = authenticationProvider;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (Logging.IsEnabled)
                Logging.Enter(this, $"{request.RequestUri}", nameof(SendAsync));

            HttpResponseMessage response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);

            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                if (request.Properties.TryGetValue(ProvisioningHeaderName, out object result))
                {
                    if (result is Action<string> setSasToken)
                    {
                        string target = GetTarget(request.RequestUri.LocalPath);
                        string responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        TpmChallenge challenge = JsonConvert.DeserializeObject<TpmChallenge>(responseContent);

                        string sasToken = ProvisioningSasBuilder.ExtractServiceAuthKey(
                            _authProvider,
                            target,
                            Convert.FromBase64String(challenge.AuthenticationKey));

                        setSasToken(sasToken);

                        if (Logging.IsEnabled)
                            Logging.Info(this, $"Authorization challenge. Retrying with Token:{sasToken}");

                        response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (Logging.IsEnabled)
                Logging.Exit(this, $"{request.RequestUri}", nameof(SendAsync));

            return response;
        }

        private static string GetTarget(string requestUriLocalPath)
        {
            requestUriLocalPath = requestUriLocalPath.TrimStart('/');
            string[] parameters = requestUriLocalPath.Split('/');
            if (parameters.Length <= 3)
            {
                throw new ArgumentException($"Invalid RequestUri LocalPath");
            }

            return string.Concat(parameters[0], "/", parameters[1], "/", parameters[2]);
        }
    }
}
