// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Azure.Devices.Shared;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Tpm2Lib;

namespace Microsoft.Azure.Devices.Provisioning.Security.Samples
{
    public class SecurityClientTpmSimulator : ProvisioningSecurityClientSasToken
    {
        private const string EmulatorAddress = "127.0.0.1";
        private const string EmulatorExeName = "Simulator.exe";
        private const int EmulatorPort = 2321;

        private Process _emulatorProcess;
        private volatile SecurityClientTpm _innerClient;
        private SemaphoreSlim _initSemaphore = new SemaphoreSlim(0);

        public SecurityClientTpmSimulator(string registrationId) : base(registrationId)
        {
        }

        public async override Task ActivateSymmetricIdentityAsync(byte[] activation)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            await _innerClient.ActivateSymmetricIdentityAsync(activation).ConfigureAwait(false);
        }

        public async override Task<byte[]> GetEndorsementKeyAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _innerClient.GetEndorsementKeyAsync().ConfigureAwait(false);
        }

        public async override Task<byte[]> GetStorageRootKeyAsync()
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _innerClient.GetStorageRootKeyAsync().ConfigureAwait(false);
        }

        public async override Task<byte[]> SignAsync(byte[] data)
        {
            await EnsureInitializedAsync().ConfigureAwait(false);
            return await _innerClient.SignAsync(data).ConfigureAwait(false);
        }

        private Task InitOnce()
        {
            return Task.Factory.StartNew(() =>
            {
                StartEmulator();

                var tpmDevice = new TcpTpmDevice(EmulatorAddress, EmulatorPort);
                tpmDevice.Connect();
                tpmDevice.PowerCycle();

                using (var tpm2 = new Tpm2(tpmDevice))
                {
                    tpm2.Startup(Su.Clear);
                }

                _innerClient = new SecurityClientTpm(RegistrationID, tpmDevice);
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Current);
        }

        private async Task EnsureInitializedAsync()
        {
            if (_innerClient == null)
            {
                try
                {
                    await _initSemaphore.WaitAsync().ConfigureAwait(false);
                    if (_innerClient == null)
                    {
                        await InitOnce().ConfigureAwait(false);
                    }
                }
                finally
                {
                    // Allow another attempt at initializing TPM.
                    _initSemaphore.Release();
                }
            }
        }

        private void StartEmulator()
        {
            foreach (var process in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(EmulatorExeName)))
            {
                try
                {
                    _emulatorProcess?.Kill();
                }
                catch (Exception)
                {
                }
            }

            string[] files = Directory.GetFiles(
                Directory.GetCurrentDirectory(), 
                EmulatorExeName, 
                SearchOption.AllDirectories);

            if (files.Length == 0)
            {
                files = Directory.GetFiles(
                    Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), 
                    EmulatorExeName, 
                    SearchOption.AllDirectories);
            }

            if (files.Length == 0)
            {
                throw new InvalidOperationException("Emulator not found");
            }

            _emulatorProcess = new Process
            {
                StartInfo =
                {
                    FileName = files[0],
                    WindowStyle = ProcessWindowStyle.Normal,
                    UseShellExecute = true
                }
            };

            _emulatorProcess.Start();
        }
    }
}
