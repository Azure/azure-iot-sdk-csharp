﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

//using Microsoft.Rest;
//using System;
//using System.Net.Http;
//using System.Net.Http.Headers;
//using System.Threading;
//using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.Provisioning.Client
{
    // Commented out until we resolve plans for TPM support and library dependency
    //internal class TpmCredentials : ServiceClientCredentials
    //{
    //    private const string SASHeaderName = "SharedAccessSignature";
    //    private volatile string _sasToken;

    //    public TpmCredentials() : base()
    //    {
    //    }

    //    public override Task ProcessHttpRequestAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    //    {
    //        if (string.IsNullOrEmpty(_sasToken))
    //        {
    //            Action<string> action = (value) =>
    //            {
    //                _sasToken = value.Replace(SASHeaderName + " ", "");
    //                SetAuthorizationHeader(request, _sasToken);
    //            };

    //            request.Properties.Add(TpmDelegatingHandler.ProvisioningHeaderName, action);
    //        }
    //        else
    //        {
    //            SetAuthorizationHeader(request, _sasToken);
    //        }

    //        return base.ProcessHttpRequestAsync(request, cancellationToken);
    //    }

    //    private static void SetAuthorizationHeader(HttpRequestMessage request, string sasToken)
    //    {
    //        request.Headers.Authorization = new AuthenticationHeaderValue(SASHeaderName, sasToken);
    //    }
    //}
}
