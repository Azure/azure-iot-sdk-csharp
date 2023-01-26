// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;

namespace Microsoft.Azure.Devices.Provisioning.Client.UnitTests
{
    [TestClass]
    [TestCategory("Unit")]
    public class RegistrationOperationStatusTests
    {
        private static readonly string s_operationId = "testing-operation-id";
        private static readonly ProvisioningRegistrationStatus s_status = ProvisioningRegistrationStatus.Assigned;
        private static readonly DeviceRegistrationResult s_registrationState = new()
        {
            RegistrationId = "testing-registration-id",
            CreatedOnUtc = DateTimeOffset.MinValue,
            AssignedHub = "testing-iot-hub",
            DeviceId = "testing-device-id",
        };

        [TestMethod]
        public void RegistrationOperationStatus_Properties()
        {
            // arrange

            var source = new RegistrationOperationStatus
            {
                OperationId = s_operationId,
                Status = s_status,
                RegistrationState = s_registrationState,
            };
            string body = JsonConvert.SerializeObject(source);

            Console.WriteLine(body);

            // act
            RegistrationOperationStatus registrationOperationStatus = JsonConvert.DeserializeObject<RegistrationOperationStatus>(body);

            // assert

            registrationOperationStatus.OperationId.Should().Be(s_operationId);
            registrationOperationStatus.Status.Should().Be(s_status);
            registrationOperationStatus.RegistrationState.RegistrationId.Should().Be("testing-registration-id");
            registrationOperationStatus.RegistrationState.CreatedOnUtc.Should().Be(DateTimeOffset.MinValue);
            registrationOperationStatus.RegistrationState.AssignedHub.Should().Be("testing-iot-hub");
            registrationOperationStatus.RegistrationState.DeviceId.Should().Be("testing-device-id");
        }
    }
}
