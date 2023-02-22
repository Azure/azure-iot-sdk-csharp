// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class QueryResponseTests
    {
        [TestMethod]
        public async Task QueryResponse_MoveNextAsync()
        {
            // arrange
            string query = "select * from devices where deviceId = 'foo'";
            var twin1 = new ClientTwin("foo");
            var twin2 = new ClientTwin("bar");
            var queryClient = new Mock<QueryClient>();

            // act
            var response = new QueryResponse<ClientTwin>(
                queryClient.Object,
                query,
                new List<ClientTwin> { twin1, twin2 },
                "",
                5);

            var expectedResponses = new List<ClientTwin> { twin1, twin2 };

            // assert
            for (int i = 0; i < expectedResponses.Count; i++)
            {
                await response.MoveNextAsync();
                ClientTwin queriedTwin = response.Current;
                queriedTwin.Should().NotBeNull();
                queriedTwin.DeviceId.Should().Be(expectedResponses[i].DeviceId);
            }
        }
    }
}
