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
        public ModuleAuthenticationWithHsm(ISignatureProvider signatureProvider, string deviceId, string moduleId, string generationId) : base(deviceId, moduleId)
        {
            _signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider));
            _generationId = generationId ?? throw new ArgumentNullException(nameof(generationId));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iotHub">IotHub hostname</param>
        /// <param name="suggestedTimeToLive">Suggested time to live seconds</param>
        /// <returns></returns>
        protected override async Task<string> SafeCreateNewToken(string iotHub, int suggestedTimeToLive)
        {
            DateTime startTime = DateTime.UtcNow;
            string audience = SasTokenBuilder.BuildAudience(iotHub, this.DeviceId, this.ModuleId);
            string expiresOn = SasTokenBuilder.BuildExpiresOn(startTime, TimeSpan.FromSeconds(suggestedTimeToLive));
            string data = string.Join("\n", new List<string> { audience, expiresOn });
            string signature = await _signatureProvider.SignAsync(this.ModuleId, this._generationId, data).ConfigureAwait(false);

            return SasTokenBuilder.BuildSasToken(audience, signature, expiresOn);
        }
    }
}
