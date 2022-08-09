// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Azure.Devices.E2ETests.IotHub.Service
{
    /// <summary>
    /// E2E test class for configurations.
    /// </summary>
    [TestClass]
    [TestCategory("E2E")]
    [TestCategory("IoTHub")]
    public class ConfigurationsClientE2ETests : E2EMsTestBase
    {
        private readonly string _idPrefix = $"{nameof(ConfigurationsClientE2ETests)}_";

        [LoggedTestMethod, Timeout(TestTimeoutMilliseconds)]
        public async Task ConfigurationOperations_Work()
        {
            // arrange

            bool configCreated = false;
            string configurationId = (_idPrefix + Guid.NewGuid()).ToLower(); // Configuration Id characters must be all lower-case.
            using var sc = new IotHubServiceClient(TestConfiguration.IoTHub.ConnectionString);

            try
            {
                var expected = new Configuration(configurationId)
                {
                    Priority = 2,
                    Labels = { { "labelName", "labelValue" } },
                    TargetCondition = "*",
                    Content = new ConfigurationContent
                    {
                        DeviceContent = { { "properties.desired.x", 4L } },
                    },
                    Metrics = new ConfigurationMetrics
                    {
                        Queries = { { "successfullyConfigured", "select deviceId from devices where properties.reported.x = 4" } }
                    },
                };

                // act and assert

                Configuration addResult = await sc.Configurations.CreateAsync(expected).ConfigureAwait(false);
                configCreated = true;
                addResult.Id.Should().Be(configurationId);
                addResult.Priority.Should().Be(expected.Priority);
                addResult.TargetCondition.Should().Be(expected.TargetCondition);
                addResult.Content.DeviceContent.First().Should().Be(expected.Content.DeviceContent.First());
                addResult.Metrics.Queries.First().Should().Be(expected.Metrics.Queries.First());
                addResult.ETag.Should().NotBeNullOrEmpty();

                Configuration getResult = await sc.Configurations.GetAsync(configurationId).ConfigureAwait(false);
                getResult.Id.Should().Be(configurationId);
                getResult.Priority.Should().Be(expected.Priority);
                getResult.TargetCondition.Should().Be(expected.TargetCondition);
                getResult.Content.DeviceContent.First().Should().Be(expected.Content.DeviceContent.First());
                getResult.Metrics.Queries.First().Should().Be(expected.Metrics.Queries.First());
                getResult.ETag.Should().Be(addResult.ETag);

                IEnumerable<Configuration> listResult = await sc.Configurations.GetAsync(100).ConfigureAwait(false);
                listResult.Should().Contain(x => x.Id == configurationId);

                expected.Priority++;
                expected.ETag = getResult.ETag;
                Configuration updateResult = await sc.Configurations.SetAsync(expected).ConfigureAwait(false);
                updateResult.Id.Should().Be(configurationId);
                updateResult.Priority.Should().Be(expected.Priority);
                updateResult.TargetCondition.Should().Be(expected.TargetCondition);
                updateResult.Content.DeviceContent.First().Should().Be(expected.Content.DeviceContent.First());
                updateResult.Metrics.Queries.First().Should().Be(expected.Metrics.Queries.First());
                updateResult.ETag.Should().NotBeNullOrEmpty().And.Should().NotBe(getResult.ETag, "The ETag should have changed after update");
            }
            finally
            {
                if (configCreated)
                {
                    // If this fails, we shall let it throw an exception and fail the test
                    await sc.Configurations.DeleteAsync(configurationId).ConfigureAwait(false);
                }
            }
        }
    }
}
