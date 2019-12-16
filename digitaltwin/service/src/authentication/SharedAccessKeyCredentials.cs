// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.Azure.Devices.DigitalTwin.Service
{
    /// <summary>
    /// Shared Access Key Signature class.
    /// </summary>
    public class SharedAccessKeyCredentials : IoTServiceClientCredentials
    {
        private static ServiceConnectionString _serviceConnectionString;

        /// <summary>
        /// Create a new instance of <code>SharedAccessKeyCredentials</code> using
        /// the Service Connection String
        /// </summary>
        public SharedAccessKeyCredentials(ServiceConnectionString serviceConnectionString)
        {
            _serviceConnectionString = serviceConnectionString;
        }

        /// <summary>
        /// Return the SAS token
        /// </summary>
        /// <returns></returns>
        protected override string GetSasToken()
        {
            return _serviceConnectionString.GetSasToken();
        }
    }
}
