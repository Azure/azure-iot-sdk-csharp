// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    /// <summary>
    /// Authentication method that uses HSM to get a SAS token.
    /// </summary>
    internal class ModuleAuthenticationWithHsm : ModuleAuthenticationWithTokenRefresh
    {
        private readonly ISignatureProvider _signatureProvider;
        private readonly string _generationId;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleAuthenticationWithHsm"/> class.
        /// </summary>
        /// <param name="signatureProvider">Provider for the token signature.</param>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="moduleId">Module Identifier.</param>
        /// <param name="generationId">The generation Id of the module. This value is used to distinguish devices with the same deviceId, when they have been deleted and re-created.</param>
        internal ModuleAuthenticationWithHsm(ISignatureProvider signatureProvider, string deviceId, string moduleId, string generationId)
            : base(deviceId, moduleId)
        {
            _signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider));
            _generationId = generationId ?? throw new ArgumentNullException(nameof(generationId));
        }

        internal ModuleAuthenticationWithHsm(
            ISignatureProvider signatureProvider,
            string deviceId,
            string moduleId,
            string generationId,
            TimeSpan sasTokenTimeToLive,
            int sasTokenRenewalBuffer,
            bool disposeWithClient)
            : base(deviceId, moduleId, (int)sasTokenTimeToLive.TotalSeconds, sasTokenRenewalBuffer, disposeWithClient)
        {
            _signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider));
            _generationId = generationId ?? throw new ArgumentNullException(nameof(generationId));
        }

        ///<inheritdoc/>
        protected override async Task<string> SafeCreateNewToken(string audience, int suggestedTimeToLive)
        {
            // DeviceId and ModuleId need to be double encoded.
            string encodedAudience = WebUtility.UrlEncode(audience);

            DateTime startTime = DateTime.UtcNow;
            string expiresOn = SasTokenBuilder.BuildExpiresOn(startTime, TimeSpan.FromSeconds(suggestedTimeToLive));
            string data = string.Join("\n", new string[] { encodedAudience, expiresOn });
            string signature = await _signatureProvider.SignAsync(ModuleId, _generationId, data).ConfigureAwait(false);

            return SasTokenBuilder.BuildSasToken(encodedAudience, signature, expiresOn);
        }
    }
}
