// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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

        // The generation Id of the module is used to distinguish devices with the same deviceId, when they have been deleted and re-created.
        internal ModuleAuthenticationWithHsm(
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
            _signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider));
            _generationId = generationId ?? throw new ArgumentNullException(nameof(generationId));
        }

        ///<inheritdoc/>
        protected override async Task<string> SafeCreateNewTokenAsync(string iotHub, TimeSpan suggestedTimeToLive)
        {
            DateTime startTime = DateTime.UtcNow;
            string audience = SasTokenBuilder.BuildAudience(iotHub, DeviceId, ModuleId);
            string expiresOn = SasTokenBuilder.BuildExpiresOn(startTime, suggestedTimeToLive);
            string data = string.Join("\n", new string[] { audience, expiresOn });
            string signature = await _signatureProvider.SignAsync(ModuleId, _generationId, data);

            return SasTokenBuilder.BuildSasToken(audience, signature, expiresOn);
        }
    }
}
