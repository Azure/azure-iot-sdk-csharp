// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Tests.HsmAuthentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class HttpRequestResponseSerializerTest
    {
        [TestMethod]
        public void TestSerializeRequest_MethodMissing_ShouldSerializeRequest()
        {
            string expected = "GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Version = Version.Parse("1.1");
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Content.Headers.TryAddWithoutValidation("content-length", "100");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string actual = Encoding.ASCII.GetString(httpRequestData);
            Assert.AreEqual(expected, actual, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestSerializeRequest_VersionMissing_ShouldSerializeRequest()
        {
            string expected = "POST /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Method = HttpMethod.Post;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Content.Headers.TryAddWithoutValidation("content-length", "100");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string actual = Encoding.ASCII.GetString(httpRequestData);
            Assert.AreEqual(expected, actual, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestSerializeRequest_ContentLengthMissing_ShouldSerializeRequest()
        {
            string expected = "POST /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 4\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Method = HttpMethod.Post;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string actual = Encoding.ASCII.GetString(httpRequestData);
            Assert.AreEqual(expected, actual, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestSerializeRequest_ContentIsNull_ShouldSerializeRequest()
        {
            string expected = "GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Method = HttpMethod.Get;

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string actual = Encoding.ASCII.GetString(httpRequestData);
            Assert.AreEqual(expected, actual, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestSerializeRequest_RequestIsNull_ShouldThrowArgumentNullException()
        {
            TestAssert.Throws<ArgumentNullException>(() => HttpRequestResponseSerializer.SerializeRequest(null));
        }

        [TestMethod]
        public void TestSerializeRequest_RequestUriIsNull_ShouldThrowArgumentNullException()
        {
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");

            TestAssert.Throws<ArgumentNullException>(() => HttpRequestResponseSerializer.SerializeRequest(request));
        }

        [TestMethod]
        public void TestSerializeRequest_ShouldSerializeRequest()
        {
            string expected = "POST /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nConnection: close\r\nHost: localhost:8081\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Version = Version.Parse("1.1");
            request.Headers.ConnectionClose = true;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Content.Headers.TryAddWithoutValidation("content-length", "100");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string actual = Encoding.ASCII.GetString(httpRequestData);
            Assert.AreEqual(expected, actual, true, CultureInfo.InvariantCulture);
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidEndOfStream_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("invalid");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<IOException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidStatusLine_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("invalid\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<HttpRequestException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidVersion_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/11 200 OK\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<HttpRequestException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidProtocolVersionSeparator_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP-1.1 200 OK\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<HttpRequestException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidStatusCode_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 2000 OK\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<ArgumentOutOfRangeException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_MissingReasonPhrase_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<HttpRequestException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidEndOfStatusMessage_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK \r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<IOException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_StatusLine_ShouldDeserialize()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            HttpResponseMessage response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            Assert.AreEqual(response.Version, Version.Parse("1.1"));
            Assert.AreEqual(response.StatusCode, System.Net.HttpStatusCode.OK);
            Assert.AreEqual(response.ReasonPhrase, "OK");
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidContentLength_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 5\r\n\r\nMessage is longer");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            TestAssert.ThrowsAsync<HttpRequestException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidHeaderSeparator_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length=5\r\n\r\nMessage is longer");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            TestAssert.ThrowsAsync<HttpRequestException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default)).Wait();
        }

        [TestMethod]
        public void TestDeserializeResponse_InvalidEndOfHeaders_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 5\r\n");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            TestAssert.ThrowsAsync<IOException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default)).Wait();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidHeader_ShouldDeserialize()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nTest-header: 4\r\n\r\nTest");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            HttpResponseMessage response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default).ConfigureAwait(false);

            Assert.AreEqual(Version.Parse("1.1"), response.Version);
            Assert.AreEqual(System.Net.HttpStatusCode.OK, response.StatusCode);
            Assert.AreEqual("OK", response.ReasonPhrase);
#if !NETCOREAPP1_1
            Assert.AreEqual(4, response.Content.Headers.ContentLength);
#endif
            Assert.AreEqual("Test", await response.Content.ReadAsStringAsync());
        }

        [TestMethod]
        public async Task TestDeserializeResponse_ValidContent_ShouldDeserialize()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 4\r\n\r\nTest");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            var response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default);

            Assert.AreEqual(response.Version, Version.Parse("1.1"));
            Assert.AreEqual(response.StatusCode, System.Net.HttpStatusCode.OK);
            Assert.AreEqual(response.ReasonPhrase, "OK");
#if !NETCOREAPP1_1
            Assert.AreEqual(response.Content.Headers.ContentLength, 4);
#endif
            Assert.AreEqual(await response.Content.ReadAsStringAsync(), "Test");
        }
    }
}
