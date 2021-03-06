// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

#if NET451
using System.Net.Http.Formatting;
#endif

namespace Microsoft.Azure.Devices.Shared
{
    internal static class HttpContentHelper
    {
#if !NET451
        private const string ApplicationJson = "application/json";
#endif

#if NET451
        private static readonly JsonMediaTypeFormatter s_jsonFormatter = new JsonMediaTypeFormatter();
#endif

        internal static void SetHttpRequestMessageContent<T>(HttpRequestMessage requestMessage, T entity)
        {
#if NET451
            requestMessage.Content = new ObjectContent<T>(entity, s_jsonFormatter);
#else
            string str = JsonConvert.SerializeObject(entity);
            requestMessage.Content = new StringContent(str, Encoding.UTF8, ApplicationJson);
#endif
        }

        internal static async Task<T> ReadHttpResponseMessageContentAsync<T>(HttpResponseMessage message, CancellationToken token)
        {
#if NET451
            T entity = await message.Content.ReadAsAsync<T>(token).ConfigureAwait(false);
#elif NET5_0
            string str = await message.Content.ReadAsStringAsync(token).ConfigureAwait(false);
            T entity = JsonConvert.DeserializeObject<T>(str);
#else
            _ = token;
            string str = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
            T entity = JsonConvert.DeserializeObject<T>(str);
#endif
            return entity;
        }

        internal static async Task WriteToStreamAsync(this Stream stream, byte[] requestBytes, CancellationToken cancellationToken)
        {
#if NET451 || NET472 || NETSTANDARD2_0
            await stream.WriteAsync(requestBytes, 0, requestBytes.Length, cancellationToken).ConfigureAwait(false);
#else
            await stream.WriteAsync(requestBytes, cancellationToken).ConfigureAwait(false);
#endif
        }

        internal static async Task CopyToStreamAsync(this HttpContent content, Stream stream, CancellationToken cancellationToken)
        {
#if NET5_0
            await content.CopyToAsync(stream, cancellationToken).ConfigureAwait(false);
#else
            _ = cancellationToken;
            await content.CopyToAsync(stream).ConfigureAwait(false);
#endif
        }

        internal static Task<Stream> ReadHttpContentAsStream(this HttpContent httpContent, CancellationToken cancellationToken)
        {
#if NET5_0
            return httpContent.ReadAsStreamAsync(cancellationToken);
#else
            _ = cancellationToken;
            return httpContent.ReadAsStreamAsync();
#endif
        }

        internal static Task<byte[]> ReadHttpContentAsByteArrayAsync(this HttpContent content, CancellationToken cancellationToken)
        {
#if NET5_0
            return content.ReadAsByteArrayAsync(cancellationToken);
#else
            _ = cancellationToken;
            return content.ReadAsByteArrayAsync();
#endif
        }

        internal static Task<string> ReadHttpContentAsStringAsync(this HttpContent content, CancellationToken cancellationToken)
        {
#if NET5_0
            return content.ReadAsStringAsync(cancellationToken);
#else
            _ = cancellationToken;
            return content.ReadAsStringAsync();
#endif
        }

    }
}