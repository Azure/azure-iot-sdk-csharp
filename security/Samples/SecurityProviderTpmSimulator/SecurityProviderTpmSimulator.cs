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
    public class SecurityProviderTpmSimulator : SecurityProviderTpm
    {
        private const string SimulatorAddress = "127.0.0.1";

        // TPM simulators available at: https://github.com/Microsoft/ms-tpm-20-ref, https://github.com/stwagnr/tpm2simulator
        private const string WindowsSimulatorExeName = "Simulator.exe";
        private const string LinuxSimulatorExeName = "/usr/local/tpm/build/Simulator";
        private const int SimulatorPort = 2321;

        private static string s_simulatorExeName;

        private static int s_simulatorProcessId;

        private TcpTpmDevice _tcpTpmDevice;
        private Tpm2 _tpm2;
        private SecurityProviderTpmHsm _innerClient;

        public SecurityProviderTpmSimulator(string registrationId)
            : base(registrationId)
        {
            _tcpTpmDevice = new TcpTpmDevice(SimulatorAddress, SimulatorPort);
            _tcpTpmDevice.Connect();
            _tcpTpmDevice.PowerCycle();

            _tpm2 = new Tpm2(_tcpTpmDevice);
            _tpm2.Startup(Su.Clear);

            _innerClient = new SecurityProviderTpmHsm(GetRegistrationID(), _tcpTpmDevice);
        }

        public override void ActivateIdentityKey(byte[] encryptedKey)
        {
            _innerClient.ActivateIdentityKey(encryptedKey);
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
            if (s_simulatorProcessId != 0)
            {
                try
                {
                    Process process = Process.GetProcessById(s_simulatorProcessId);
                    process.Kill();
                }
                catch (ArgumentException)
                {
                    // Process not found
                }
                finally
                {
                    s_simulatorProcessId = 0;
                }
            }
        }

        public static void StartSimulatorProcess()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                s_simulatorExeName = WindowsSimulatorExeName;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                s_simulatorExeName = LinuxSimulatorExeName;
            }
            else
            {
                throw new PlatformNotSupportedException(
                    "TSS.MSR Simulator.exe is available only for Windows. On Linux, ensure that the simulator is " +
                    $"started and listening to TCP connections on {SimulatorAddress}:{SimulatorPort}.");
            }

            // Exe is found at the exact specified path.
            if (Process.GetProcessesByName(Path.GetFileNameWithoutExtension(s_simulatorExeName)).Length > 0) return;

            // Search next to the simulator DLL location.
            string[] files = Directory.GetFiles(
                Directory.GetCurrentDirectory(),
                s_simulatorExeName,
                SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                files = Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                    s_simulatorExeName,
                    SearchOption.AllDirectories);
            }

            if (files.Length == 0)
            {
                throw new InvalidOperationException($"TPM Simulator not found: {s_simulatorExeName}");
            }

            using var simulatorProcess = new Process
            {
                StartInfo =
                {
                    FileName = files[0],
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = true,
                }
            };

            simulatorProcess.Start();
            s_simulatorProcessId = simulatorProcess.Id;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tcpTpmDevice?.Dispose();
                _tcpTpmDevice = null;

                _tpm2?.Dispose();
                _tpm2 = null;

                _innerClient?.Dispose();
                _innerClient = null;
            }
        }
    }
}
