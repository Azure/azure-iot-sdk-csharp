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
            var buffered = new HttpBufferedStream(memory);

            var allLines = new List<string>(5);

            while (true)
            {
                string currentLine = await buffered.ReadLineAsync(default).ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(currentLine))
                {
                    break;
                }
                allLines.Add(currentLine);
            }

            Assert.AreEqual(5, allLines.Count);
            Assert.AreEqual("GET /modules/testModule/sign?api-version=2018-06-28 HTTP/1.1", allLines[0]);
            Assert.AreEqual("Host: localhost:8081", allLines[1]);
            Assert.AreEqual("Connection: close", allLines[2]);
            Assert.AreEqual("Content-Type: application/json", allLines[3]);
            Assert.AreEqual("Content-Length: 100", allLines[4]);
        }
    }
}
