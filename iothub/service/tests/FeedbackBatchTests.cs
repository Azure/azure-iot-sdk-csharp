// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Azure.Amqp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests
{
    [TestClass]
    [TestCategory("Unit")]
    public class FeedbackBatchTests
    {
        [TestMethod]
        public void FeedBackBatch_ctor_ok()
        {
            using var amqpMessage = AmqpMessage.Create();
            FeedbackRecord[] feedbackRecords = {
                new FeedbackRecord(),
                new FeedbackRecord()
            };
            var feedbackBatch = new FeedbackBatch
            {
                EnqueuedOnUtc = (DateTime)amqpMessage.MessageAnnotations.Map[MessageSystemPropertyNames.EnqueuedOn],
                Records = feedbackRecords,
                IotHubHostName = Encoding.UTF8.GetString(
                    amqpMessage.Properties.UserId.Array,
                    amqpMessage.Properties.UserId.Offset,
                    amqpMessage.Properties.UserId.Count)
            };
        }

    }
}
