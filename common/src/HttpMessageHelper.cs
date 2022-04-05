// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A helper class to simplify operations with Http messages based on the .NET implementation used.
    /// </summary>
    internal static class HttpMessageHelper
    {

        private const string ApplicationJson = "application/json";

        internal static void SetHttpRequestMessageContent<T>(HttpRequestMessage requestMessage, T entity)
        {
            string str = JsonConvert.SerializeObject(entity);
            requestMessage.Content = new StringContent(str, Encoding.UTF8, ApplicationJson);
        }
    }
}