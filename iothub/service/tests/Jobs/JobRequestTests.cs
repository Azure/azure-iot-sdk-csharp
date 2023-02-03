// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.Tests.Jobs
{
    [TestClass]
    [TestCategory("Unit")]
    public class JobRequestTests
    {
        [TestMethod]
        public void JobRequest_FieldInitialization()
        {
            // arrange
            var request = new JobRequest();

            // act
            long? maxExecutionTimeInSeconds = request.MaxExecutionTimeInSeconds;

            // assert
            maxExecutionTimeInSeconds.Should().Be(null);

            // rearrange
            request.MaxExecutionTimeInSeconds = 5L;

            // assert
            request.MaxExecutionTimeInSeconds.Should().Be(5L);
        }
    }
}
