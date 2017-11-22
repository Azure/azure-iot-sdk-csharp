// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;

namespace Microsoft.Azure.Devices.Provisioning.Client.Transport
{
    public class ProvisioningTransportRegisterMessage
    {
        private ProductInfo _productInfo = new ProductInfo();

        public string GlobalDeviceEndpoint { get; private set; }

        public string IdScope { get; private set; }

        public SecurityClient Security { get; private set; }

        public string ProductInfo
        {
            get
            {
                return _productInfo.ToString();
            }
            set
            {
                _productInfo.Extra = value;
            }
        }

        public ProvisioningTransportRegisterMessage(
            string globalDeviceEndpoint,
            string idScope,
            SecurityClient security)
        {
            GlobalDeviceEndpoint = globalDeviceEndpoint;
            IdScope = idScope;
            Security = security;
        }
    }
}
