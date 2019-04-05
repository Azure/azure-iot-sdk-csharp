// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Threading.Tasks;

namespace Microsoft.Azure.Devices.E2ETests
{
    public static class TwinOperation
    {
        private static TestLogging s_log = TestLogging.GetInstance();

        public static async Task Twin_DeviceSetsReportedPropertyAndGetsItBack(DeviceClient deviceClient)
        {
            var propName = Guid.NewGuid().ToString();
            var propValue = Guid.NewGuid().ToString();

            TwinCollection props = new TwinCollection();
            props[propName] = propValue;
            await deviceClient.UpdateReportedPropertiesAsync(props).ConfigureAwait(false);

            Twin deviceTwin = await deviceClient.GetTwinAsync().ConfigureAwait(false);
            Assert.AreEqual<String>(deviceTwin.Properties.Reported[propName].ToString(), propValue);
        }

        public static async Task RegistryManagerUpdateDesiredPropertyAsync(string deviceId, string propName, string propValue)
        {
            using (RegistryManager registryManager = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString))
            {
                var twinPatch = new Twin();
                twinPatch.Properties.Desired[propName] = propValue;

                await registryManager.UpdateTwinAsync(deviceId, twinPatch, "*").ConfigureAwait(false);
                await registryManager.CloseAsync().ConfigureAwait(false);
            }
        }

        public static async Task<Task> SetTwinPropertyUpdateCallbackHandlerAsync(DeviceClient deviceClient, string expectedPropName, string expectedPropValue)
        {
            var propertyUpdateReceived = new TaskCompletionSource<bool>();
            string userContext = "myContext";

            await deviceClient.SetDesiredPropertyUpdateCallbackAsync(
                (patch, context) =>
                {
                    s_log.WriteLine($"{nameof(SetTwinPropertyUpdateCallbackHandlerAsync)}: DesiredProperty: {patch}, {context}");

                    try
                    {
                        Assert.AreEqual(expectedPropValue, patch[expectedPropName].ToString());
                        Assert.AreEqual(userContext, context, "Context");
                    }
                    catch (Exception e)
                    {
                        propertyUpdateReceived.SetException(e);
                    }
                    finally
                    {
                        propertyUpdateReceived.SetResult(true);
                    }

                    return Task.FromResult<bool>(true);
                }, userContext).ConfigureAwait(false);

            return propertyUpdateReceived.Task;
        }
    }
}
