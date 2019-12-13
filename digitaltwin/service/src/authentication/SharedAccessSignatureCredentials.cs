// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Rest;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Azure.IoT.DigitalTwin.Service
{
    /// <summary>
    /// Shared Access Key Signature class.
    /// </summary>
    public class SharedAccessSignatureCredentials : IoTServiceClientCredentials
    {
        private string _sasKey;

        /// <summary>
        /// Create a new instance of <code>SharedAccessKeyCredentials</code> using
        /// the Shared Access Key
        /// </summary>
        public SharedAccessSignatureCredentials(string sharedAccessSignature)
        {
            _sasKey = sharedAccessSignature;
        }

        /// <summary>
        /// Return the SAS token
        /// </summary>
        /// <returns></returns>
        protected override string GetSasToken()
        {
            return _sasKey;
        }
    }
}
