// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Threading.Tasks;
using CommandLine;

namespace CertificateSigningRequestSample;

/// <summary>
/// Simple IoT device sample using certificate authentication.
/// 
/// This sample connects to IoT Hub using MQTT, requests a new certificate
/// via the credential management API, and sends telemetry messages.
/// </summary>
public static class Program
{
    /// <summary>
    /// Main entry point for the Certificate Signing Request sample.
    /// </summary>
    /// <param name="args">
    /// Run with `--help` to see a list of required and optional parameters.
    /// </param>
    /// <returns>0 on success, 1 on failure.</returns>
    public static async Task<int> Main(string[] args)
    {
        Parameters? parameters = null;
        ParserResult<Parameters> result = Parser.Default.ParseArguments<Parameters>(args)
            .WithParsed(parsedParams =>
            {
                parameters = parsedParams;
            })
            .WithNotParsed(errors =>
            {
                Environment.Exit(1);
            });

        if (parameters == null)
        {
            return 1;
        }

        var sample = new CertificateSigningRequestSample(parameters);
        return await sample.RunSampleAsync();
    }
}

