﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

// If you see intermittent failures on devices that are created by this file, check to see if you have multiple suites 
// running at the same time because one test run could be accidentally destroying devices created by a different test run.

namespace Microsoft.Azure.Devices.E2ETests
{
    public class TestUtil
    {
        public const string FaultType_Tcp = "KillTcp";
        public const string FaultType_AmqpConn = "KillAmqpConnection";
        public const string FaultType_AmqpSess = "KillAmqpSession";
        public const string FaultType_AmqpCBSReq = "KillAmqpCBSLinkReq";
        public const string FaultType_AmqpCBSResp = "KillAmqpCBSLinkResp";
        public const string FaultType_AmqpD2C = "KillAmqpD2CLink";
        public const string FaultType_AmqpC2D = "KillAmqpC2DLink";
        public const string FaultType_AmqpTwinReq = "KillAmqpTwinLinkReq";
        public const string FaultType_AmqpTwinResp = "KillAmqpTwinLinkResp";
        public const string FaultType_AmqpMethodReq = "KillAmqpMethodReqLink";
        public const string FaultType_AmqpMethodResp = "KillAmqpMethodRespLink";
        public const string FaultType_Throttle = "InvokeThrottling";
        public const string FaultType_QuotaExceeded = "InvokeMaxMessageQuota";
        public const string FaultType_Auth = "InvokeAuthError";
        public const string FaultType_GracefulShutdownAmqp = "ShutDownAmqp";
        public const string FaultType_GracefulShutdownMqtt = "ShutDownMqtt";

        public const string FaultCloseReason_Boom = "Boom";
        public const string FaultCloseReason_Bye = "byebye";

        public const int DefaultDelayInSec = 1;
        public const int DefaultDurationInSec = 5;

        public const int ShortRetryInMilliSec = 3000;

        public static int EventHubEpoch = 0;

        public static string GetHostName(string connectionString)
        {
            Regex regex = new Regex("HostName=([^;]+)", RegexOptions.None);
            return regex.Match(connectionString).Groups[1].Value;
        }

        public static string GetDeviceConnectionString(Device device, string hostName)
        {
            var connectionString = new StringBuilder();
            connectionString.AppendFormat("HostName={0}", hostName);
            connectionString.AppendFormat(";DeviceId={0}", device.Id);
            connectionString.AppendFormat(";SharedAccessKey={0}", device.Authentication.SymmetricKey.PrimaryKey);
            return connectionString.ToString();
        }

        public static Tuple<string, RegistryManager> InitializeEnvironment(string devicePrefix)
        {
            // TODO: #222 - E2E tests improper use of TPL.
            RegistryManager rm = GetRegistryManagerAsync(devicePrefix).GetAwaiter().GetResult();
            return new Tuple<string, RegistryManager>(Configuration.IoTHub.ConnectionString, rm);
        }

        public static async Task<RegistryManager> GetRegistryManagerAsync(string devicePrefix)
        {
            RegistryManager rm = RegistryManager.CreateFromConnectionString(Configuration.IoTHub.ConnectionString);
            await RemoveDevicesAsync(devicePrefix, rm);

            return rm;
        }

        private static async Task RemoveDevicesAsync(string devicePrefix, RegistryManager rm)
        {
            Console.WriteLine($"{nameof(RemoveDeviceAsync)} Enumerating devices.");
            IEnumerable<Device> devices = await rm.GetDevicesAsync(int.MaxValue).ConfigureAwait(false);

            // Ensure to remove all previous devices.
            foreach (Device device in devices)
            {
                if (device.Id.StartsWith(devicePrefix))
                {
                    Console.WriteLine($"{nameof(RemoveDeviceAsync)} Removing {device.Id}.");
                    await RemoveDeviceAsync(device.Id, rm).ConfigureAwait(false);
                }
            }
        }

        public static Task UnInitializeEnvironment(RegistryManager rm)
        {
            return rm.CloseAsync();
        }

        public static Tuple<string, string> CreateDevice(string devicePrefix, string hostName, RegistryManager registryManager)
        {
            string deviceName = null;
            string deviceConnectionString = null;

            Task.Run(async () =>
            {
                deviceName = devicePrefix + Guid.NewGuid();
                Console.WriteLine($"Creating device {deviceName} on {hostName}");
                var device = await registryManager.AddDeviceAsync(new Device(deviceName));
                deviceConnectionString = TestUtil.GetDeviceConnectionString(device, hostName);
                Console.WriteLine("Device successfully created");
            }).Wait();

            Thread.Sleep(1000);
            return new Tuple<string, string>(deviceName, deviceConnectionString);
        }

        public static Tuple<string, string> CreateDeviceWithX509(string devicePrefix, string hostName, RegistryManager registryManager)
        {
            string deviceName = null;

            Task.Run(async () =>
            {
                deviceName = devicePrefix + Guid.NewGuid();
                Console.WriteLine($"Creating device X509 {deviceName} on {hostName}");
                var device1 = new Device(deviceName)
                {
                    Authentication = new AuthenticationMechanism()
                    {
                        X509Thumbprint = new X509Thumbprint()
                        {
                            PrimaryThumbprint = Configuration.IoTHub.GetCertificateWithPrivateKey().Thumbprint
                        }
                    }
                };

                var device = await registryManager.AddDeviceAsync(device1);
                Console.WriteLine("Device successfully created");
            }).Wait();

            Thread.Sleep(1000);
            return new Tuple<string, string>(deviceName, hostName);
        }

        public static async Task RemoveDeviceAsync(string deviceName, RegistryManager registryManager)
        {
            Console.WriteLine("Removing device " + deviceName);
            await registryManager.RemoveDeviceAsync(deviceName);
            Console.WriteLine("Device successfully removed");
        }

        public static Client.Message ComposeErrorInjectionProperties(string faultType, string reason, int delayInSecs, int durationInSecs = 0)
        {
            string dataBuffer = Guid.NewGuid().ToString();

            return new Client.Message(Encoding.UTF8.GetBytes(dataBuffer))
            {
                Properties =
                {
                    ["AzIoTHub_FaultOperationType"] = faultType,
                    ["AzIoTHub_FaultOperationCloseReason"] = reason,
                    ["AzIoTHub_FaultOperationDelayInSecs"] = delayInSecs.ToString(),
                    ["AzIoTHub_FaultOperationDurationInSecs"] = durationInSecs.ToString()
                }
            };
        }
    }
}
