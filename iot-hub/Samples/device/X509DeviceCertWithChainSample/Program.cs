// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using CommandLine;
using Microsoft.Azure.Devices.Client;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace X509DeviceCertWithChainSample
{
    public class Program
    {
        /// <summary>
        /// A sample to illustrate authenticating with a device by passing in the device certificate and 
        /// full chain of certificates from the one used to sign the device certificate to the one uploaded to the service.
        /// AuthSetup.ps1 can be used to create the necessary certs and setup to run this sample.
        /// </summary>
        /// <param name="args">
        /// Run with `--help` to see a list of required and optional parameters.
        /// </param>
        public static async Task<int> Main(string[] args)
        {
            // Parse application parameters
            Parameters parameters = null;
            ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
                .WithParsed(parsedParams =>
                {
                    parameters = parsedParams;
                })
                .WithNotParsed(errors =>
                {
                    Environment.Exit(1);
                });

            var chainCerts = new X509Certificate2Collection();
            chainCerts.Add(new X509Certificate2(parameters.RootCertPath));
            chainCerts.Add(new X509Certificate2(parameters.Intermediate1CertPath));
            chainCerts.Add(new X509Certificate2(parameters.Intermediate2CertPath));
            var deviceCert = new X509Certificate2(parameters.DevicePfxPath, parameters.DevicePfxPassword);
            var auth = new DeviceAuthenticationWithX509Certificate(parameters.DeviceName, deviceCert, chainCerts);

            using var deviceClient = DeviceClient.Create(
                parameters.HostName,
                auth,
                parameters.TransportType);

            var sample = new X509DeviceCertWithChainSample(deviceClient);
            await sample.RunSampleAsync();
            await deviceClient.CloseAsync();

            return 0;
        }
    }
}
