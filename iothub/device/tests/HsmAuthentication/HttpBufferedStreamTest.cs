// Copyright(c) Microsoft.All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Devices.Client.HsmAuthentication.Transport;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Client.Test.HsmAuthentication
{
    [TestClass]
    [TestCategory("Unit")]
    public class HttpBufferedStreamTest
    {
        [TestMethod]
        public async Task TestReadLines_ShouldReturnResponse()
        {
            string expected = "GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1\r\nHost: localhost:8081\r\nConnection: close\r\nContent-Type: application/json\r\nContent-Length: 100\r\n\r\n";

            byte[] expectedBytes = Encoding.UTF8.GetBytes(expected);
            var memory = new MemoryStream(expectedBytes, true);

            IList<string> lines = new List<string>();
            var buffered = new HttpBufferedStream(memory);
            System.Threading.CancellationToken cancellationToken = default(System.Threading.CancellationToken);
            string line = await buffered.ReadLineAsync(cancellationToken);

            while (!string.IsNullOrEmpty(line))
            {
                lines.Add(line);
                line = await buffered.ReadLineAsync(cancellationToken);
            }

            Assert.AreEqual(5, lines.Count);
            Assert.AreEqual("GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1", lines[0]);
            Assert.AreEqual("Host: localhost:8081", lines[1]);
            Assert.AreEqual("Connection: close", lines[2]);
            Assert.AreEqual("Content-Type: application/json", lines[3]);
            Assert.AreEqual("Content-Length: 100", lines[4]);
        }

    }
}