// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    /// <summary>
    /// Authentication method that uses HSM to get a SAS token.
    /// </summary>
    internal class EdgeModuleAuthenticationWithHsm : AuthenticationWithTokenRefresh
    {
        private readonly ISignatureProvider _signatureProvider;
        private readonly string _generationId;

        // The generation Id of the module is used to distinguish devices with the same deviceId, when they have been deleted and re-created.
        internal EdgeModuleAuthenticationWithHsm(
            ISignatureProvider signatureProvider,
            string deviceId,
            string moduleId,
            string generationId,
            TimeSpan sasTokenTimeToLive = default,
            int sasTokenRenewalBuffer = default)
            : base(
                deviceId,
                moduleId,
                sasTokenTimeToLive,
                sasTokenRenewalBuffer)
        {
            Debug.Assert(signatureProvider != null, $"{nameof(signatureProvider)} cannot be null. Validate argument upstream.");
            Debug.Assert(generationId != null, $"{nameof(generationId)} cannot be null. Validate argument upstream.");

            _signatureProvider = signatureProvider;
            _generationId = generationId;
        }

        ///<inheritdoc/>
        protected override async Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            DateTime startTime = DateTime.UtcNow;
            string audience = SharedAccessSignatureBuilder.BuildAudience(iotHub, DeviceId, ModuleId);
            string expiresOn = SharedAccessSignatureBuilder.BuildExpiresOn(suggestedTimeToLive, startTime);
            string data = string.Join("\n", new string[] { audience, expiresOn });
            string signature = await _signatureProvider.SignAsync(ModuleId, _generationId, data).ConfigureAwait(false);

            return SharedAccessSignatureBuilder.BuildSignature(audience, signature, expiresOn);
        }
    }
}
