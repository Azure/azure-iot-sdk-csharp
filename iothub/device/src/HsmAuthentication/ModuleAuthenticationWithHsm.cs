// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Client.HsmAuthentication
{
    /// <summary>
    /// Authentication method that uses HSM to get a SAS token. 
    /// </summary>
    public class ModuleAuthenticationWithHsm : ModuleAuthenticationWithTokenRefresh
    {
        readonly ISignatureProvider signatureProvider;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModuleAuthenticationWithHsm"/> class.
        /// </summary>
        /// <param name="signatureProvider">Provider for the token signature.</param>
        /// <param name="deviceId">Device Identifier.</param>
        /// <param name="moduleId">Module Identifier.</param>
        public ModuleAuthenticationWithHsm(ISignatureProvider signatureProvider, string deviceId, string moduleId) : base(deviceId, moduleId)
        {
            this.signatureProvider = signatureProvider ?? throw new ArgumentNullException(nameof(signatureProvider)); 
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
            string signature = await signatureProvider.SignAsync(this.ModuleId, data);

            return SasTokenBuilder.BuildSasToken(audience, signature, expiresOn, this.ModuleId);
        }
    }
}
