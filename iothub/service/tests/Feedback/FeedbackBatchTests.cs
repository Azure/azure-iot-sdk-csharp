// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Azure.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Feedback
{
    [TestClass]
    [TestCategory("Unit")]
    public class FeedbackBatchTests
    {
        [TestMethod]
        public void FeedBackBatch_PropertySetsAndGets()
        {
            // arrange
            FeedbackRecord[] feedbackRecords = 
            {
                new FeedbackRecord(),
                new FeedbackRecord()
            };

            var expectedEnqueuedOnUtc = new DateTime(2008, 5, 1, 8, 30, 52);
            string expectedHubName = "testhub";

            var feedbackBatch = new FeedbackBatch
            {
                EnqueuedOnUtc = expectedEnqueuedOnUtc,
                Records = feedbackRecords,
                IotHubHostName = expectedHubName
            };

            // assert
            feedbackBatch.EnqueuedOnUtc.Should().Be(expectedEnqueuedOnUtc);
            feedbackBatch.Records.Should().HaveCount(feedbackRecords.Length);
            feedbackBatch.IotHubHostName.Should().Be(expectedHubName);
        }
    }
}
