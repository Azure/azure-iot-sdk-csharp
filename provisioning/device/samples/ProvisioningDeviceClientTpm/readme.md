# Provisioning Device Client Sample - TPM Attestation

## Overview

This is a quick tutorial with the steps to register a device in the Microsoft Azure IoT Hub Device Provisioning Service using the Trusted Platform Module (TPM) attestation.

## How to run the sample

Ensure that all prerequisite steps presented in [samples](../) have been performed.
To run the sample, in a developer command prompt enter:

`dotnet run <IDScope>`

replacing `IDScope` with the value found within the Device Provisioning Service Overview tab. E.g. `dotnet run 0ne1234ABCD`

Continue by following the instructions presented by the sample.

The sample is currently using `SecurityProviderTpmSimulator` which is not supported on Linux. To run against a real TPM2.0 device, replace this with `SecurityProviderTpm`.
