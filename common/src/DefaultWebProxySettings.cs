// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Net;

namespace Microsoft.Azure.Devices
{
    /// <summary>
    /// WebProxy class for initializing default web proxy
    /// </summary>
    internal sealed class DefaultWebProxySettings : IWebProxy
    {
        private static readonly DefaultWebProxySettings s_defaultWebproxy = new DefaultWebProxySettings();
        public static DefaultWebProxySettings Instance { get; } = s_defaultWebproxy;
        public ICredentials Credentials { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public Uri GetProxy(Uri destination) => throw new NotSupportedException();
        public bool IsBypassed(Uri host) => throw new NotSupportedException();
    }
}
