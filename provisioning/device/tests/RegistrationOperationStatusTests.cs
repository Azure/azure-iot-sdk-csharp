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
        private const string FakeOperationId = "testing-operation-id";

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
                OperationId = FakeOperationId,
                Status = s_status,
                RegistrationState = s_registrationState,
            };
            string body = JsonConvert.SerializeObject(source);

            // act
            RegistrationOperationStatus registrationOperationStatus = JsonConvert.DeserializeObject<RegistrationOperationStatus>(body);

            // assert

            registrationOperationStatus.OperationId.Should().Be(source.OperationId);
            registrationOperationStatus.Status.Should().Be(source.Status);
            registrationOperationStatus.RegistrationState.RegistrationId.Should().Be(source.RegistrationState.RegistrationId);
            registrationOperationStatus.RegistrationState.CreatedOnUtc.Should().Be(source.RegistrationState.CreatedOnUtc);
            registrationOperationStatus.RegistrationState.AssignedHub.Should().Be(source.RegistrationState.AssignedHub);
            registrationOperationStatus.RegistrationState.DeviceId.Should().Be(source.RegistrationState.DeviceId);
        }
    }
}
