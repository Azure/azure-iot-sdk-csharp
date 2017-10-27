// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Security.Samples
{
    /// <summary>
    /// Implements a TPMv2 Simulator based on the TSS.MSR Simulator.
    /// This code is provides as a sample to enable provisioning on hardware without an actual hardware TPM device and
    /// provides no real security.
    /// </summary>
    public class SecurityClientTpmSimulator : SecurityClientHsmTpm
    {
        private const string SimulatorAddress = "127.0.0.1";
        private const string SimulatorExeName = "Simulator.exe";
        private const int SimulatorPort = 2321;

        private SecurityClientTpm _innerClient;

        public SecurityClientTpmSimulator(string registrationId) : base(registrationId)
        {
            var tpmDevice = new TcpTpmDevice(SimulatorAddress, SimulatorPort);
            tpmDevice.Connect();
            tpmDevice.PowerCycle();

            using (var tpm2 = new Tpm2(tpmDevice))
            {
                tpm2.Startup(Su.Clear);
            }

            _innerClient = new SecurityClientTpm(GetRegistrationID(), tpmDevice);
        }

        public override void ActivateSymmetricIdentity(byte[] activation)
        {
            _innerClient.ActivateSymmetricIdentity(activation);
        }

        public override byte[] GetEndorsementKey()
        {
            return _innerClient.GetEndorsementKey();
        }

        public override byte[] GetStorageRootKey()
        {
            return _innerClient.GetStorageRootKey();
        }

        public override byte[] Sign(byte[] data)
        {
            return _innerClient.Sign(data);
        }

        public static void StopSimulatorProcess()
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(SimulatorExeName)))
            {
                try
                {
                    process?.Kill();
                }
                catch (Exception)
                {
                }
            }
        }

        public static void StartSimulatorProcess()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                throw new PlatformNotSupportedException(
                    "TSS.MSR Simulator.exe is available only for Windows. On Linux, ensure that the simulator is " + 
                    $"started and listening to TCP connections on {SimulatorAddress}:{SimulatorPort}.");
            }

            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(SimulatorExeName)).Length > 0) return;
            
            string[] files = Directory.GetFiles(
                Directory.GetCurrentDirectory(), 
                SimulatorExeName, 
                SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                files = Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                    SimulatorExeName, 
                    SearchOption.AllDirectories);
            }

            if (files.Length == 0)
            {
                throw new InvalidOperationException($"TPM Simulator not found : {SimulatorExeName}");
            }

            var simulatorProcess = new Process
            {
                StartInfo =
                {
                    FileName = files[0],
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = true
                }
            };

            simulatorProcess.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _innerClient.Dispose();
            }
        }
    }
}
