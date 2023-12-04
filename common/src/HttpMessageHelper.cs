// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net.Http;
using System.Text;
using Newtonsoft.Json;

#if NET451
using System.Net.Http.Formatting;
#endif

namespace Microsoft.Azure.Devices.Shared
{
    /// <summary>
    /// A helper class to simplify operations with Http messages based on the .NET implementation used.
    /// </summary>
    internal static class HttpMessageHelper
    {
#if NET451
        private static readonly JsonMediaTypeFormatter s_jsonFormatter = new JsonMediaTypeFormatter();
#else
        private const string ApplicationJson = "application/json";
#endif

        internal static void SetHttpRequestMessageContent<T>(HttpRequestMessage requestMessage, T entity)
        {
#if NET451
            requestMessage.Content = new ObjectContent<T>(entity, s_jsonFormatter);
#else
            string str = JsonConvert.SerializeObject(entity, JsonSerializerSettingsInitializer.GetJsonSerializerSettings());
            requestMessage.Content = new StringContent(str, Encoding.UTF8, ApplicationJson);
#endif
        }
    }
}
