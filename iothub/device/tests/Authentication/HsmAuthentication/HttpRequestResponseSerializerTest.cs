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
using FluentAssertions;
using System.Threading;
using System.Net;
using System.Runtime.InteropServices;

namespace Microsoft.Azure.Devices.Client.Test.HsmAuthentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class HttpRequestResponseSerializerTest
    {
        [TestMethod]
        public void TestSerializeRequest_MethodMissing_ShouldSerializeRequest()
        {
            string input = "GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Version = Version.Parse("1.1");
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Content.Headers.TryAddWithoutValidation("content-length", "100");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string requestString = Encoding.ASCII.GetString(httpRequestData);

            // assert
            var newLines = new[] { '\r', '\n' };
            var actual = requestString.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            var expected = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            actual.Length.Should().Be(expected.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                actual[i].Should().BeEquivalentTo(expected[i]);
            }
        }

        [TestMethod]
        public void TestSerializeRequest_VersionMissing_ShouldSerializeRequest()
        {
            string input = "POST /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Method = HttpMethod.Post;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Content.Headers.TryAddWithoutValidation("content-length", "100");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string requestString = Encoding.ASCII.GetString(httpRequestData);

            // assert
            var newLines = new[] { '\r', '\n' };
            var actual = requestString.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            var expected = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            actual.Length.Should().Be(expected.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                actual[i].Should().BeEquivalentTo(expected[i]);
            }
        }

        [TestMethod]
        public void TestSerializeRequest_ContentLengthMissing_ShouldSerializeRequest()
        {
            string input = "POST /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 4\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Method = HttpMethod.Post;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string requestString = Encoding.ASCII.GetString(httpRequestData);

            // assert
            var newLines = new[] { '\r', '\n' };
            var actual = requestString.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            var expected = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            actual.Length.Should().Be(expected.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                actual[i].Should().BeEquivalentTo(expected[i]);
            }
        }

        [TestMethod]
        public void TestSerializeRequest_ContentIsNull_ShouldSerializeRequest()
        {
            string input = "GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Method = HttpMethod.Get;

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string requestString = Encoding.ASCII.GetString(httpRequestData);

            // assert
            var newLines = new[] { '\r', '\n' };
            var actual = requestString.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            var expected = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            actual.Length.Should().Be(expected.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                actual[i].Should().BeEquivalentTo(expected[i]);
            }
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
            string input = "POST /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nConnection: close\r\nHost: localhost:8081\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";
            HttpRequestMessage request = new HttpRequestMessage();
            request.Method = HttpMethod.Post;
            request.RequestUri = new Uri("http://localhost:8081/modules/testModule/sign?api-version=2018-06-28", UriKind.Absolute);
            request.Version = Version.Parse("1.1");
            request.Headers.ConnectionClose = true;
            request.Content = new ByteArrayContent(Encoding.UTF8.GetBytes("test"));
            request.Content.Headers.TryAddWithoutValidation("content-type", "application/json");
            request.Content.Headers.TryAddWithoutValidation("content-length", "100");

            byte[] httpRequestData = HttpRequestResponseSerializer.SerializeRequest(request);
            string requestString = Encoding.ASCII.GetString(httpRequestData);

            // assert
            var newLines = new[] { '\r', '\n' };
            var actual = requestString.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            var expected = input.Split(newLines, StringSplitOptions.RemoveEmptyEntries);
            actual.Length.Should().Be(expected.Length);
            for (int i = 0; i < actual.Length; ++i)
            {
                actual[i].Should().BeEquivalentTo(expected[i]);
            }
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidEndOfStream_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("invalid");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidStatusLine_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("invalid\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidVersion_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/11 200 OK\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidProtocolVersionSeparator_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP-1.1 200 OK\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidStatusCode_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 2000 OK\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            await TestAssert.ThrowsAsync<ArgumentOutOfRangeException>(() => HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken));
        }

        [TestMethod]
        public async Task TestDeserializeResponse_MissingReasonPhrase_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidEndOfStatusMessage_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK \r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_StatusLine_ShouldDeserialize()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\n\r\n");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            HttpResponseMessage response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            response.Version.Should().Be(Version.Parse("1.1"));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ReasonPhrase.Should().Be("OK");
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidContentLength_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 5\r\n\r\nMessage is longer");
            var memory = new MemoryStream(expected, true);
            HttpBufferedStream stream = new HttpBufferedStream(memory);

            CancellationToken cancellationToken = default(System.Threading.CancellationToken);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, cancellationToken);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidHeaderSeparator_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length=5\r\n\r\nMessage is longer");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidEndOfHeaders_ShouldThrow()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 5\r\n");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            // act
            Func<Task> act = async () => await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default);

            // assert
            var error = await act.Should().ThrowAsync<IotHubClientException>();
            error.And.ErrorCode.Should().Be(IotHubClientErrorCode.NetworkErrors);
            error.And.IsTransient.Should().BeTrue();
        }

        [TestMethod]
        public async Task TestDeserializeResponse_InvalidHeader_ShouldDeserialize()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nTest-header: 4\r\n\r\nTest");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            HttpResponseMessage response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default).ConfigureAwait(false);

            response.Version.Should().Be(Version.Parse("1.1"));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ReasonPhrase.Should().Be("OK");
#if !NETCOREAPP1_1
            response.Content.Headers.ContentLength.Should().Be(4);
#endif
            (await response.Content.ReadAsStringAsync()).Should().Be("Test");
        }

        [TestMethod]
        public async Task TestDeserializeResponse_ValidContent_ShouldDeserialize()
        {
            byte[] expected = Encoding.UTF8.GetBytes("HTTP/1.1 200 OK\r\nContent-length: 4\r\n\r\nTest");
            var memory = new MemoryStream(expected, true);
            var stream = new HttpBufferedStream(memory);

            var response = await HttpRequestResponseSerializer.DeserializeResponseAsync(stream, default);

            response.Version.Should().Be(Version.Parse("1.1"));
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.ReasonPhrase.Should().Be("OK");
#if !NETCOREAPP1_1
            response.Content.Headers.ContentLength.Should().Be(4);
#endif
            (await response.Content.ReadAsStringAsync()).Should().Be("Test");
        }
    }
}
