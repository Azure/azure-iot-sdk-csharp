// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Azure.Iot.DigitalTwin.Device;
using Azure.Iot.DigitalTwin.Device.Model;
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Tasks;
using Xunit;

namespace Azure.IoT.DigitalTwin.E2ETests.interfaces
{
    /// <summary>
    /// Abstract test interface representation. Tracks all commands invoked, all properties updated, and if onRegistrationComplete was fired
    /// </summary>
    abstract class DigitalTwinInterfaceClientWithTracking : DigitalTwinInterfaceClient
    {
        public ConcurrentDictionary<string, string> CommandsInvoked { get; private set; }

        public ConcurrentBag<DigitalTwinPropertyUpdate> PropertyUpdates { get; private set; }

        public bool onRegistrationCompleteExecuted { get; private set; }

        public DigitalTwinInterfaceClientWithTracking(string interfaceId, string interfaceName)
            : base(interfaceId, interfaceName)
        {
            CommandsInvoked = new ConcurrentDictionary<string, string>();
            PropertyUpdates = new ConcurrentBag<DigitalTwinPropertyUpdate>();
            onRegistrationCompleteExecuted = false;
        }

        protected override async Task<DigitalTwinCommandResponse> OnCommandRequest(DigitalTwinCommandRequest commandRequest)
        {
            string invokedCommandName = commandRequest.Name;
            CommandsInvoked.TryAdd(invokedCommandName, commandRequest.RequestId);

            return await OnDelegatedCommandRequest(commandRequest);
        }

        protected abstract Task<DigitalTwinCommandResponse> OnDelegatedCommandRequest(DigitalTwinCommandRequest commandRequest);

        public void AssertCommandCalled(string commandName, string serviceSideRequestId)
        {
            if (CommandsInvoked.ContainsKey(commandName))
            {
                string deviceSideRequestId = CommandsInvoked[commandName];
                Assert.Equal(deviceSideRequestId, serviceSideRequestId);
            }
            else
            {
                throw new Exception("Expected command " + commandName + " to be called on digital twin client, but was never called");
            }
        }

        public void AssertCommandNotCalled(string commandName)
        {
            if (CommandsInvoked.ContainsKey(commandName))
            {
                throw new Exception("Expected command " + commandName + " to not be called on digital twin client, but it was");
            }
        }

        protected override async Task OnPropertyUpdated(DigitalTwinPropertyUpdate propertyUpdate)
        {
            PropertyUpdates.Add(propertyUpdate);
        }

        public bool propertyWasUpdated(string propertyName, string expectedPropertyValue, int timeoutSeconds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed.Seconds < timeoutSeconds)
            {
                foreach (DigitalTwinPropertyUpdate propertyUpdate in PropertyUpdates)
                {
                    if (propertyUpdate.PropertyName.Equals(propertyName))
                    {
                        if (("\"" + expectedPropertyValue + "\"").Equals(propertyUpdate.PropertyDesired))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void AssertPropertyWasNotUpdated(string propertyName, string expectedPropertyValue, int timeoutSeconds)
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();

            while (stopwatch.Elapsed.Seconds < timeoutSeconds)
            {
                foreach (DigitalTwinPropertyUpdate propertyUpdate in PropertyUpdates)
                {
                    if (propertyUpdate.PropertyName.Equals(propertyName))
                    {
                        Assert.Equal("\"" + expectedPropertyValue + "\"", propertyUpdate.PropertyDesired);
                        return;
                    }
                }
            }

            throw new Exception("Digital twin device never received a property update for property name " + propertyName);
        }

        protected override void OnRegistrationCompleted()
        {
            onRegistrationCompleteExecuted = true;
        }

        public async Task sendTelemetryAsync(string telemetryName, string telemetryValue)
        {
            await this.SendTelemetryAsync(telemetryName, telemetryValue);
        }
    }
}
